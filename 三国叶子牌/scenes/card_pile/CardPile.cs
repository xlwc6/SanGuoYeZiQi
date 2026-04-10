using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 卡堆，本游戏卡堆是可见的，更像是个不可操作的手牌器，所以存储的是Card，否则存CardData比较好
/// </summary>
public partial class CardPile : HBoxContainer, IDeck
{
    [Export]
    private OwnerType ownerType;
    [Export]
    private CardPileType pileType;
    [Export]
    private Marker2D markPosition;

    public List<Card> cards = [];  

    public override void _Ready()
    {
        if (pileType == CardPileType.Tiled)
        {
            // 平铺：像手牌管理器一样，每个子项的间隔为-90
            Set("theme_override_constants/separation", -90);
        }
        else
        {
            // 堆叠：所有卡牌按Z排序，这里好像可以简单实现，每个子项的间隔为-130（卡牌宽度）
            Set("theme_override_constants/separation", -130);
        }
    }

    public void AddCard(Card card)
    {
        if (card == null) return;
        card.ownerType = ownerType;
        card.SetBackShow(false);
        card.ChangedToState(BaseState.State.Discard);
        AddChild(card);
        cards.Add(card);
    }

    public void RemoveCard(Card card)
    {
        if (ownerType != card.ownerType) return;
        cards.Remove(card);
        RemoveChild(card);
    }

    public Card GetRandomCard(CardData.CampType camp)
    {
        var meets = cards.Where(x => x.cardData.Camp == camp).ToList();
        if (meets == null || meets.Count == 0)
        {
            return null;
        }
        if (meets.Count == 1)
        {
            var card = meets[0];
            card.ChangedToState(BaseState.State.Normal);
            return card;
        }
        else
        {
            Random random = new Random();
            int index = random.Next(meets.Count);
            var card = meets[index];
            card.ChangedToState(BaseState.State.Normal);
            return card;
        }
    }
    
    public void Clear()
    {
        foreach (var item in GetChildren())
        {
            item.QueueFree();
        }
        cards.Clear();
    }

    public Vector2 GetMarkPosition()
    {
        return markPosition.GlobalPosition;
    }

    public List<CardData> GetCardDatas()
    {
        var list = new List<CardData>();
        foreach (var item in cards)
        {
            list.Add(item.cardData);
        }
        return list;
    }
}
