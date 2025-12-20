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
    private List<(Suit,Rank)> _deck = new List<(Suit,Rank)>();
    private List<MagicCard> _magicCard = new List<MagicCard>();
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
        CalculateScore(selectedCards, handType);
        int totalScore = _baseChips * _multiplier;
        _currentScore += totalScore;
        _handsLeft--;
        GD.Print($"Played {handType}: {_baseChips} chips x {_multiplier} mult = {totalScore} points");
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
            NewStage();
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
        
    }
    private void CloseShopPressed()
    {
        
    }
    private void GameOver()
    {
        
    }
    private void SortSwitch()
    {
        _sortByRank=!_sortByRank;
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
        TypeLabel.Text = $"Hand Type : {handType}";
        ScoreLabel.Text = $"Score : {_currentScore}/{_targetScore}";
        ChipsLabel.Text = $"Chips : {_baseChips}";
        MultLabel.Text = $"Multiplier : {_multiplier}";
        StageLabel.Text = $"Stage : {_currentStage}";
        HandLabel.Text = $"Hands : {_handsLeft}";
        DiscardLabel.Text = $"Discards : {_discardLeft}";
        MoneyLabel.Text = $"Money : {_money}";
    }
    
}
