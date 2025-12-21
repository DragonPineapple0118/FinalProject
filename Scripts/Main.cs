using CardData;
using Godot;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading;

public partial class Main : Control
{
    [Export] public int DrawCardCount = 7;
    [Export] public PackedScene CardObject;
    [Export] public Control HandContainer;
    [Export] public Control JokerContainer;
    [Export] public Label ScoreLabel;
    [Export] public Label ChipsLabel;
    [Export] public Label MultLabel;
    [Export] public Label StageLabel;
    [Export] public Label HandLabel;
    [Export] public Label DiscardLabel;
    [Export] public Label MoneyLabel;
    [Export] public Label TypeLabel;
    [Export] public Button PlayHandButton;
    [Export] public Button DiscardButton;
    [Export] public Control ShopPanel;
    [Export] public Control ShopItemContainer;
    [Export] public Button CloseShopButton;
    [Export] public Button SortToggleButton;
    [Export] public Panel GameOverPanel;
    [Export] public Label GameOverScoreLabel;
    [Export] public Button RetryButton;
    [Export] public Label BonusLabel;

    private List<(Suit,Rank)> _deck = new List<(Suit,Rank)>();
    private List<MagicCard> _magicCard = new List<MagicCard>();
    private Dictionary<HandType, int> _handBonuses = new Dictionary<HandType,int>();
    private List<Suit> _SuitChanges = new List<Suit>();
    private int _currentScore = 0;
    private int _targetScore = 55;
    private int _currentStage = 1;
    private int _handsLeft = 4;
    private int _discardLeft = 3;
    private int _money = 4;
    private int _baseChips = 0;
    private int _multiplier = 0;
    public int _selectCount = 0;
    private bool _shopOpen = false;
    private bool _sortByRank = true;
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("Ready");
        PlayHandButton.Pressed += PlayHandPressed;
        DiscardButton.Pressed += DiscardPressed;
        CloseShopButton.Pressed += CloseShopPressed;
        SortToggleButton.Pressed += SortSwitch;
        RetryButton.Pressed += Retry;
        Initialize();
        NewStage();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
    
    private void Initialize()
    {
        GD.Print("Initializing");
        _deck.Clear();
        foreach(Suit s in Enum.GetValues(typeof(Suit)))
        {
            foreach(Rank r in Enum.GetValues(typeof(Rank)))
            {
                _deck.Add((s,r));
            }
        }
        GD.Print($"Deck initialized with {_deck.Count} cards");
    }
    
    private void NewStage()
    {
        _currentScore = 0;
        _handsLeft = 4;
        _discardLeft = 3;
        _targetScore = 300+(_currentStage-1)*150;
        GD.Print($"Starting Stage {_currentStage}");
        Initialize();
        ShuffleDeck();
        ClearHand();
        DrawCard(DrawCardCount);
        UpdateUI();
        ShopPanel.Visible = false;
    }
    
    private void CalculateScore(List<Card> cards,HandType handType)
    {
        _baseChips = handType switch
        {
            HandType.Single => 1,
            HandType.Pair => 2,
            HandType.TwoPair => 3,
            HandType.Straight => 5,
            HandType.Flush => 6,
            HandType.FullHouse => 8,
            HandType.FourOfAKind => 10,
            HandType.StraightFlush => 12,
            _ => 0
        };
        
        // Apply magic card hand bonuses
        if (_handBonuses.ContainsKey(handType))
        {
            _baseChips += _handBonuses[handType];
            GD.Print($"Applied bonus to {handType}: +{_handBonuses[handType]} chips");
        }
        
        List<Card> ContributingCards = GetContributingCards(cards ,handType);
        foreach (var card in ContributingCards)
        {
            _baseChips += GetCardChips(card._rank);
        }
        
        _multiplier = handType switch
        {
            HandType.Single => 1,
            HandType.Pair => 2,
            HandType.TwoPair => 3,
            HandType.Straight => 4,
            HandType.Flush => 4,
            HandType.FullHouse => 5,
            HandType.FourOfAKind => 6,
            HandType.StraightFlush => 7,
            _ => 0
        };
    }
    
    private List<Card> GetContributingCards(List<Card> cards, HandType handtype)
    {
        var contributingCards = new List<Card>();

        switch (handtype)
        {
            case HandType.Single:
                contributingCards.Add(cards.OrderByDescending(c => c._rank).First());
                break;
            case HandType.Pair:
                var pairrank = GetPairRank(cards);
                contributingCards.AddRange(cards.Where(c => c._rank == pairrank));
                break;
            case HandType.TwoPair:
                var pairRanks = GetTwoPairRank(cards);
                contributingCards.AddRange(cards.Where(c => pairRanks.Contains(c._rank)));
                break;
            case HandType.Straight:
                contributingCards.AddRange(cards);
                break;
            case HandType.Flush:
                contributingCards.AddRange(cards);
                break;
            case HandType.StraightFlush:
                contributingCards.AddRange(cards);
                break;
            case HandType.FullHouse:
                contributingCards.AddRange(cards);
                break;
            case HandType.FourOfAKind:
                var fourOfAKindRank = GetFourOfAKindRank(cards);
                contributingCards.AddRange(cards.Where(c => c._rank == fourOfAKindRank));
                break;
        }
        return contributingCards;
    }

    private Rank GetPairRank(List<Card> cards)
    {
        return cards.GroupBy(c => c._rank).Where(g => g.Count() >= 2).Select(g => g.Key).FirstOrDefault();
    }

    private List<Rank> GetTwoPairRank(List<Card> cards)
    {
        return cards.GroupBy(c => c._rank).Where(g => g.Count() >= 2).Select(g => g.Key).Take(2).ToList();
    }

    private Rank GetFourOfAKindRank(List<Card> cards)
    {
        return cards.GroupBy(c => c._rank).Where(g => g.Count() == 4).Select(g => g.Key).FirstOrDefault();
    }
    
    public List<Card> GetSelectedCards()
    {
        List<Card> selectedCard = new List<Card>();
        foreach (var c in HandContainer.GetChildren())
        {
            if(c is Card card &&card.IsSelected)
                selectedCard.Add(card);
        }
        return selectedCard;
    }
    
    private int GetCardChips(Rank rank)
    {
        return rank switch
        {
            Rank.Two => 12,
            Rank.Three => 3,
            Rank.Four => 4,
            Rank.Five => 5,
            Rank.Six => 6,
            Rank.Seven => 7,
            Rank.Eight => 8,
            Rank.Nine => 9,
            Rank.Ten => 10,
            Rank.Jack => 10,
            Rank.Queen => 10,
            Rank.King => 10,
            Rank.Ace => 11,
            _ => 0
        };
    }
    
    private void PlayHandPressed()
    {
        List<Card> selectedCards = GetSelectedCards();
        if (selectedCards.Count()==0) return;
        HandType handType = HandEvaluator.Evaluate(selectedCards);
        double _stageEffect =1;
        if(_currentStage == 2)
        {
            _stageEffect = handType switch
            {
                HandType.Single => 0.5,
                HandType.Pair => 2,
                _ => 1
            };
        }
        CalculateScore(selectedCards, handType);
        int totalScore = (int)(_baseChips * _multiplier * _stageEffect);
        _currentScore += totalScore;
        _handsLeft--;
        if(_stageEffect==1)
            GD.Print($"Played {handType}: {_baseChips} chips x {_multiplier} mult = {totalScore} points");
        else 
            GD.Print($"Played {handType}: {_baseChips} chips x {_multiplier} mult x {_stageEffect} Stage Effect = {totalScore} points");
        
        int DecreasedCardCount = selectedCards.Count();
        foreach(var card in selectedCards)
            card.QueueFree();
        DrawCard(DrawCardCount - (HandContainer.GetChildCount() - DecreasedCardCount));
        UpdateUI();
        IfWin();
    }
    
    private void IfWin()
    {
        if(_currentScore >= _targetScore)
        {
            GD.Print($"Stage {_currentStage} Completed!");
            _currentStage ++;
            _money += 5 + _handsLeft;
            OpenShop();
        }
        else if(_handsLeft == 0)
        {
            GD.Print($"Game Over! Final Score: {_currentScore}/{_targetScore}");
            GameOver();
        }
    }
    
    private void DiscardPressed()
    {
        if (_discardLeft <= 0)
        {
            GD.Print("No Discard Count Left");
            return;
        }
        List<Card> selectedCards = GetSelectedCards();
        if (selectedCards.Count==0)return;
        _discardLeft --;
        int DecreasedCardCount = selectedCards.Count();
        foreach(var card in selectedCards)
            card.QueueFree();
        DrawCard(DrawCardCount - (HandContainer.GetChildCount() - DecreasedCardCount));
        UpdateUI();
    }
    
    private void OpenShop()
    {
        ShopPanel.Visible = true;
        GenerateShopItems();
    }
    
    private void GenerateShopItems()
    {
        foreach(Node child in ShopItemContainer.GetChildren())
            child.QueueFree();
            
        var Cards = new List<MagicCard>
        {
            new HandScoreUpgrade(HandType.Single,2),
            new HandScoreUpgrade(HandType.Pair,3),
            new HandScoreUpgrade(HandType.TwoPair,4),
            new HandScoreUpgrade(HandType.Straight,5),
            new HandScoreUpgrade(HandType.Flush,5),
            new HandScoreUpgrade(HandType.FullHouse,6),
            new HandScoreUpgrade(HandType.FourOfAKind,7),
            new HandScoreUpgrade(HandType.StraightFlush,8),
            new SuitChange(Suit.Hearts),
            new SuitChange(Suit.Diamonds),
            new SuitChange(Suit.Clubs),
            new SuitChange(Suit.Spades),
        };
        
        var random = new Random();
        foreach(var magicCard in Cards.OrderBy(x => random.Next()).Take(2).ToList())
        {
            CreateShopItem(magicCard);
        }
    }
    
    private void CreateShopItem(MagicCard magicCard)
    {
        var itemContainer = new VBoxContainer();
        itemContainer.CustomMinimumSize = new Vector2(280, 120);
        ShopItemContainer.AddChild(itemContainer);
        
        // Add background panel
        var panel = new Panel();
        panel.Size = new Vector2(280, 120);
        itemContainer.AddChild(panel);
        
        var nameLabel = new Label();
        nameLabel.Text = magicCard.Name;
        nameLabel.AddThemeStyleboxOverride("normal", new StyleBoxFlat());
        itemContainer.AddChild(nameLabel);
        
        var descLabel = new Label();
        descLabel.Text = magicCard.Description;
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        descLabel.CustomMinimumSize = new Vector2(260, 40);
        itemContainer.AddChild(descLabel);
        
        var priceLabel = new Label();
        priceLabel.Text = "Price: $3";
        itemContainer.AddChild(priceLabel);
        
        var buyButton = new Button();
        buyButton.Text = _money >= 3 ? "Buy" : "Can't Afford";
        buyButton.Disabled = _money < 3;
        buyButton.Pressed += () => BuyMagicCard(magicCard, itemContainer);
        itemContainer.AddChild(buyButton);
    }
    
    private void BuyMagicCard(MagicCard magicCard, Node itemContainer)
    {
        if(_money >= 3)
        {
            _money -= 3;
            _magicCard.Add(magicCard);
            GD.Print($"Buying magic card: {magicCard.Name}");
            magicCard.ApplyEffect(this);
            itemContainer.QueueFree();
            UpdateUI();
            UpdateMagicCardDisplay();
            GD.Print($"Bought {magicCard.Name}: {magicCard.Description}");
        }
        else 
            GD.Print("No Enough Money");
    }
    
    private void CloseShopPressed()
    {
        ShopPanel.Visible = false;
        NewStage();
    }
    
    private void GameOver()
    {
        GameOverPanel.Visible = true;
        GameOverScoreLabel.Text = $"Final Score\n{_currentScore}/{_targetScore}";
        PlayHandButton.Disabled = true;
        DiscardButton.Disabled = true;
        SortToggleButton.Disabled = true;
    }
    
    private void Retry()
    {
        _currentStage = 1;
        _currentScore = 0;
        _handsLeft = 5;
        _discardLeft = 3;
        _money = 4;
        _magicCard.Clear();
        _handBonuses.Clear();
        _SuitChanges.Clear();
        GameOverPanel.Visible = false;
        PlayHandButton.Disabled = false;
        DiscardButton.Disabled = false;
        SortToggleButton.Disabled = false;
        NewStage();
    }
    
    private void SortSwitch()
    {
        _sortByRank=!_sortByRank;
        SortToggleButton.Text = _sortByRank ? "Sort : Rank" : "Sort : Suit";
        Sort();
    }
    
    public void AddHandBonus(HandType handType, int bonus)
    {
        if(_handBonuses.ContainsKey(handType))
            _handBonuses[handType] += bonus;
        else
            _handBonuses[handType] = bonus;
        GD.Print($"Added hand bonus: {handType} +{bonus} chips (total: {_handBonuses[handType]})");
    }
    
    public void EnableSuitChange(Suit suit)
    {
        _SuitChanges.Add(suit);
        foreach (Node child in HandContainer.GetChildren())
        {
            if(child is Card card && card._suit != suit)
            {
                card.EnableSuitChange(suit);
            }
        }
        GD.Print($"Suit Change to {suit} is enabled");
    }
    
    private void ShuffleDeck()
    {
        Random random = new Random();
        int n = _deck.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            var value = _deck[k];
            _deck[k] = _deck[n];
            _deck[n] = value;
        }
    }
    
    private void ClearHand()
    {
        foreach (Node child in HandContainer.GetChildren())
            child.QueueFree();
        _selectCount = 0;
    }
    
    private void DrawCard(int n)
    {
        for(int i = 0; i < n; i++)
        {
            var data = _deck[0];
            _deck.RemoveAt(0);
            Card NewCard = CardObject.Instantiate<Card>();
            HandContainer.AddChild(NewCard);
            NewCard.SetData(data.Item1,data.Item2);
            
            // Apply suit changes to new cards if available
            foreach (var targetSuit in _SuitChanges)
            {
                if (NewCard._suit != targetSuit)
                {
                    NewCard.EnableSuitChange(targetSuit);
                }
            }
            
            NewCard.OnSelectedChanged += ()=>
            {
                if(GetSelectedCards().Count > 5)
                    NewCard.SetSelected();
                UpdateUI();
            };
        }
    }
    
    private void Sort()
    {
        var cards =new List<Card>();
        foreach (Node child in HandContainer.GetChildren())
        {
            var card = child as Card;
            cards.Add(card);
        }
        if(cards.Count==0)return;
        var sortedCards = new List<Card>(cards);
        if (_sortByRank)
        {
            sortedCards.Sort((a,b) => b._rank.CompareTo(a._rank));
        }
        DoSortCards(sortedCards);
    }
    
    private void DoSortCards(List<Card> sortedCards)
    {
        foreach (var card in sortedCards)
        {
            HandContainer.RemoveChild(card);
        }
        foreach(var card in sortedCards)
        {
            HandContainer.AddChild(card);
        }
    }
    
    private void UpdateUI()
    {
        Sort();
        List<Card> selectedCards = GetSelectedCards();
        HandType handType = HandEvaluator.Evaluate(selectedCards);
        CalculateScore(selectedCards,handType);
        
        string handBonusText = "";
        if (_handBonuses.ContainsKey(handType) && _handBonuses[handType] > 0)
        {
            handBonusText = $" (+{_handBonuses[handType]} bonus)";
        }
        
        TypeLabel.Text = $"{handType}";
        ScoreLabel.Text = $"{_currentScore}/{_targetScore}";
        ChipsLabel.Text = $"{_baseChips}";
        MultLabel.Text = $"{_multiplier}";
        StageLabel.Text = $"Stage{_currentStage}";
        HandLabel.Text = $"{_handsLeft}";
        DiscardLabel.Text = $"{_discardLeft}";
        BonusLabel.Text = $"{handBonusText}";
        MoneyLabel.Text = $"{_money}";
    }
    
    private void UpdateMagicCardDisplay()
    {
        // Clear existing magic card display
        foreach (Node child in JokerContainer.GetChildren())
            child.QueueFree();
            
        GD.Print($"Updating magic card display. Total cards: {_magicCard.Count}");
        
        // Display owned magic cards
        foreach (var magicCard in _magicCard)
        {
            var cardLabel = new Label();
            cardLabel.Text = $"{magicCard.Name}\n{magicCard.Description}";
            cardLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            cardLabel.CustomMinimumSize = new Vector2(120, 80);
            JokerContainer.AddChild(cardLabel);
            GD.Print($"Displaying magic card: {magicCard.Name}");
        }
    }
}