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