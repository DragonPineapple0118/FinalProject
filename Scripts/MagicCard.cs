using Godot;
using CardData;
public abstract class MagicCard
{
    public string Name ;
    public string Description;
    
    public abstract void ApplyEffect(Main game);
}

public class HandScoreUpgrade : MagicCard
{
    public HandType TargetHandType;
    public int BonusChips;

    public HandScoreUpgrade(HandType handType, int bonus)
    {
        TargetHandType = handType;
        BonusChips = bonus;
        Name = $"Upgrade {handType}";
        Description = $"+{bonus} chips to {handType}";
    }
    public override void ApplyEffect(Main game)
    {
        game.AddHandBonus(TargetHandType, BonusChips);
    }
}

public class SuitChange : MagicCard
{
    public Suit ToSuit ;
    public SuitChange(Suit suit)
    {
        ToSuit = suit;
        Name = $"Suit Changer";
        Description = $"Change one card to {suit} (right click)";
    }
    public override void ApplyEffect(Main game)
    {
        game.EnableSuitChange(ToSuit, this);
    }
}

// Card Multiplier Magic Card
public class CardMultiplier : MagicCard
{
    public Rank TargetRank { get; private set; }
    public float Multiplier { get; private set; }
    
    public CardMultiplier(Rank rank, float multiplier)
    {
        TargetRank = rank;
        Multiplier = multiplier;
        Name = $"{rank} Multiplier";
        Description = $"{rank} cards score x{multiplier}";
    }
    
    public override void ApplyEffect(Main game)
    {
        game.AddCardMultiplier(TargetRank, Multiplier);
    }
}

// Discard/Redraw Magic Card
public class DiscardRedraw : MagicCard
{
    public DiscardRedraw()
    {
        Name = "Fresh Hand";
        Description = "Discard all cards and draw new hand (one-time use)";
    }
    
    public override void ApplyEffect(Main game)
    {
        game.EnableDiscardRedraw(this);
    }
}

// Draw Boost Magic Card
public class DrawBoost : MagicCard
{
    public int ExtraCards { get; private set; }
    
    public DrawBoost(int extraCards)
    {
        ExtraCards = extraCards;
        Name = "Draw Boost";
        Description = $"Increase hand size by +{extraCards}";
    }
    
    public override void ApplyEffect(Main game)
    {
        game.AddDrawBoost(ExtraCards);
    }
}

// Extra Hands Magic Card
public class ExtraHands : MagicCard
{
    public int HandsCount { get; private set; }
    
    public ExtraHands(int extraHands)
    {
        HandsCount = extraHands;
        Name = "Extra Hands";
        Description = $"Start each stage with +{extraHands} extra hands";
    }
    
    public override void ApplyEffect(Main game)
    {
        game.AddExtraHands(HandsCount);
    }
}

// Extra Discards Magic Card
public class ExtraDiscards : MagicCard
{
    public int DiscardsCount { get; private set; }
    
    public ExtraDiscards(int extraDiscards)
    {
        DiscardsCount = extraDiscards;
        Name = "Extra Discards";
        Description = $"Start each stage with +{extraDiscards} extra discards";
    }
    
    public override void ApplyEffect(Main game)
    {
        game.AddExtraDiscards(DiscardsCount);
    }
}