using Godot;
using CardData;
using System;
using System.ComponentModel;

public partial class Card : Button 
{
	private Control _cardVisual; 
	private ShaderMaterial _cardMaterial;
	// 平滑移動的變數----------------------------
    private float _targetRotX = 0f;
    private float _targetRotY = 0f;
    private float _currentRotX = 0f;
    private float _currentRotY = 0f;
    private Vector2 _basePosition; 
    [Export] public float SmoothSpeed = 10.0f;
	// -----------------------------------------
	[Export] public float AngleXMax = 15.0f; 
    [Export] public float AngleYMax = 15.0f; 
	[Export] public float FollowSpeed = 15.0f;
	[Export] public TextureRect  CardFace;
	[Export] public SubViewportContainer CardComp;
	[Export] public Label RankLable;
	[Export] public Label RankLable2;
	[Export] public TextureRect SuitImage;
	[Export] public TextureRect CardBack;
	[Export] public TextureRect Shadow;
	[Export] public Vector2 CardSize = new Vector2(192, 270);
	public Suit _suit ;
	public Rank _rank;
	private bool CanChangeSuit;
	private Suit ChangeSuitTo;
	public bool IsSelected = false;
	private Vector2 _originalPosition;
	private bool _hovering = false;
	private bool _mousePressed = false; // 追蹤滑鼠是否按住
	public event Action OnSelectedChanged;
	public event Action<Suit, Suit> OnSuitChanged;

	public override void _Ready()
	{
		_cardVisual = CardComp;	
		_cardVisual.GlobalPosition = new Vector2(1200,700);
		_basePosition = _cardVisual.GlobalPosition;
		_cardVisual.TopLevel = true;
		_cardVisual.GuiInput += OnVisualGuiInput;
		CardComp.MouseEntered += MouseHover;
		CardComp.MouseExited += MouseExite;
		var originalMat = _cardVisual.Material as ShaderMaterial;

		//問了AI要怎麼把shader獨立出來
			// 為了安全起見，檢查一下是不是真的有抓到材質
			if (originalMat != null)
			{
			// 2. 使用 Duplicate() 創造一份全新的副本
			// 這會讓 _cardMaterial 變成一個獨立的記憶體實體
				_cardMaterial = (ShaderMaterial)originalMat.Duplicate();
			// 3. 【最重要的一步】把這份「新副本」塞回去給 _cardVisual
			// 如果漏了這行，雖然你複製了材質，但卡片顯示時還是會用舊的那份共用材質
				_cardVisual.Material = _cardMaterial;
			}
	}

	public override void _Process(double delta)
	{
		float fDelta = (float)delta;
		
		_basePosition = GlobalPosition;
		//原本寫的檢測滑鼠是不是在卡片上的方法有bug 問AI有沒有別的方法
			// 使用更精確的滑鼠檢測 - 檢查滑鼠是否在CardVisual範圍內
			bool mouseInCard = false;
			if (_cardVisual != null)
			{
				Rect2 cardRect = new Rect2(_cardVisual.GlobalPosition, _cardVisual.Size);
				mouseInCard = cardRect.HasPoint(GetGlobalMousePosition());
			}
		
		Vector2 targetOffset = Vector2.Zero;
		Vector2 shadowTargetOffset = new Vector2(10,10);
		if (IsSelected)
		{
			targetOffset += new Vector2(0, -60);
			shadowTargetOffset = new Vector2(10,-20);
		}
		//同上
			// 改進的hover檢測邏輯：
			// 1. 如果沒有按住滑鼠，使用正常的hover狀態
			// 2. 如果按住滑鼠，直接檢查滑鼠是否在卡片上
		bool shouldHover = _mousePressed ? mouseInCard : _hovering;
		if (shouldHover)
		{
			targetOffset += new Vector2(0, -30);
		}

		Vector2 finalTargetPos = _basePosition + targetOffset;
        if(_cardVisual != null){
            _cardVisual.GlobalPosition = _cardVisual.GlobalPosition.Lerp(finalTargetPos, fDelta * FollowSpeed);
            Shadow.GlobalPosition = Shadow.GlobalPosition.Lerp(_basePosition+shadowTargetOffset, fDelta * FollowSpeed);
        }
		//卡片傾斜效果是yt上學的:)
        if (_cardMaterial != null)
        {
            _currentRotX = Mathf.Lerp(_currentRotX, _targetRotX, fDelta * FollowSpeed);
            _currentRotY = Mathf.Lerp(_currentRotY, _targetRotY, fDelta * FollowSpeed);

            _cardMaterial.SetShaderParameter("x_rot", _currentRotX);
            _cardMaterial.SetShaderParameter("y_rot", _currentRotY);
        }
	}
	private void OnVisualGuiInput(InputEvent @event)
	{
		if(@event is InputEventMouseMotion)
		{
			HandleTiltEffect(_cardVisual.GetLocalMousePosition());
		}
		if(@event is InputEventMouseButton mouseEvent) //同上點擊問題請AI改的 防止selected之後hover消失
		{
			if(mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					_mousePressed = true;
					CardPressed();
				}
				else
				{
					_mousePressed = false;
					// 當滑鼠釋放時，檢查是否還在卡片上來決定是否保持hover效果
					if (_cardVisual != null)
					{
						Rect2 cardRect = new Rect2(_cardVisual.GlobalPosition, _cardVisual.Size);
						bool mouseStillInCard = cardRect.HasPoint(GetGlobalMousePosition());
						if (!mouseStillInCard)
						{
							ResetTilt();
						}
					}
				}
			}
			else if(mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed && CanChangeSuit)
			{
				ChangeSuit();
			}
		}
	}
	public void SetData(Suit s, Rank r)
	{
		_rank = r;
		_suit = s;
		string path = $"res://CardFace/{s.ToString().ToLower()}.png";//問了AI怎麼改外觀跟顏色
		Color color = (s==Suit.Hearts||s==Suit.Diamonds)?Colors.Red:Colors.Black;
		if (RankLable != null)
		{
			RankLable.Text = GetRankText(r);
			RankLable.Modulate = color;
			RankLable2.Text = GetRankText(r);
			RankLable2.Modulate = color;
		}
		if (SuitImage != null)
		{
			SuitImage.Texture = GD.Load<Texture2D>(path);
			SuitImage.Modulate = color;
		}
		if(CardBack != null)//保留沒有用到背面
		{
			CardBack.Visible= false;
		}
	}

	private string GetRankText(Rank r)
	{
		return r switch
		{
			Rank.Jack => "J",
			Rank.Queen => "Q",
			Rank.King => "K",
			Rank.Ace => "A",
			Rank.Two => "2",
			_ => ((int)r).ToString()
		};
	}
	private void MouseHover()
	{
		_hovering = true;
		this.ZIndex=10;
	}
	private void MouseExite()
	{
		_hovering = false;
		this.ZIndex=1;
		ResetTilt();
	}
	private void HandleTiltEffect(Vector2 mousePos)
    {
		//從yt學過來的:)
        float lerpValX = Mathf.Remap(mousePos.X, 0, CardSize.X, 0, 1);
        float lerpValY = Mathf.Remap(mousePos.Y, 0, CardSize.Y, 0, 1);
        _targetRotY = Mathf.Lerp(-AngleYMax, AngleYMax, lerpValX);
        _targetRotX = Mathf.Lerp(AngleXMax, -AngleXMax, lerpValY);
    }

    private void ResetTilt()
    {
        _targetRotX = 0f;
        _targetRotY = 0f;
    }
	private void CardPressed()
	{	
		IsSelected = !IsSelected;
		OnSelectedChanged?.Invoke();
	}
	public void SetSelected()
	{
		IsSelected = false;
	}
	public void EnableSuitChange(Suit suit)
	{
		CanChangeSuit = true;
		ChangeSuitTo = suit;
		GD.Print($"Card {_rank} of {_suit} can now be changed to {suit} (right-click)");
	}
	private void ChangeSuit()
	{
		if(!CanChangeSuit)return;
		Suit oldSuit = _suit;
		_suit = ChangeSuitTo;
		CanChangeSuit = false;
		SetData(_suit,_rank);
		GD.Print($"Suit changed from {oldSuit} to {_suit}");
		OnSuitChanged?.Invoke(oldSuit, _suit);
	}
	
	public void DisableSuitChange(Suit suit)
	{
		if (ChangeSuitTo == suit)
		{
			CanChangeSuit = false;
			GD.Print($"Disabled suit change to {suit} for card {_rank} of {_suit}");
		}
	}
}