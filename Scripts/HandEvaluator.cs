using System.Collections.Generic;
using System.Linq;//AI建議使用的
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using CardData;
using Godot;

public static class HandEvaluator
{
    public static HandType Evaluate(List<Card> cardNodes)
    {
        if(cardNodes == null || cardNodes.Count ==0)
            return HandType.None;
        var cards = cardNodes.OrderBy(card => card._rank).ToList();
        if(cards.Count == 5)
        {
            if(IsStriaghtFlush(cards)) return HandType.StraightFlush;
            if(IsFourOfAKind(cards)) return HandType.FourOfAKind;
            if(IsFullHouse(cards)) return HandType.FullHouse;
            if(IsFlush(cards)) return HandType.Flush;
            if(IsStraight(cards)) return HandType.Straight;
            if(IsTwoPair(cards)) return HandType.TwoPair;
            if(IsPair(cards)) return HandType.Pair;
            return HandType.Single;
        }
        if(cards.Count == 4)
        {
           if(IsTwoPair(cards)) return HandType.TwoPair;
           if(IsPair(cards)) return HandType.Pair;
           return HandType.Single;
        }
        if(cards.Count == 3)
        {
            if(IsPair(cards))return HandType.Pair;
            return HandType.Single;
        }
        if(cards.Count == 2)
        {
            if(IsPair(cards))return HandType.Pair;
            return HandType.Single;
        }
        if(cards.Count == 1)
        {
            return CardData.HandType.Single;
        }
        return HandType.None;
    }
    private static bool IsPair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(card => card._rank).Select(g => g.Count()).ToList();
        return rankGroups.Contains(2);
    }
    private static bool IsTwoPair(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(cards => cards._rank).Select(g => g.Count()).ToList();
        return rankGroups.Count(X => X == 2) == 2;
    }
    private static bool IsFlush(List<Card> cards)
    {
        var suit = cards[0]._suit;
        return cards.All(c=>c._suit == suit);
    }
    private static bool IsStraight(List<Card> cards)
    {
        for(int i = 1; i < 5; i++)
        {
            if((int)cards[i]._rank !=(int)cards[i-1]._rank+1)
                return false;
        }
        return true;
    }
    private static bool IsFullHouse(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c=>c._rank).Select(g=>g.Count()).OrderBy(x=>x).ToList();
        return rankGroups.Count == 2 && rankGroups[0] == 2 && rankGroups[1] == 3;
    }
    private static bool IsFourOfAKind(List<Card> cards)
    {
        var rankGroups = cards.GroupBy(c => c._rank).Select(g => g.Count()).ToList();
        return rankGroups.Contains(4);
    }
    private static bool IsStriaghtFlush(List<Card> cards)
    {
        return IsStraight(cards) && IsFlush(cards);
    }

}