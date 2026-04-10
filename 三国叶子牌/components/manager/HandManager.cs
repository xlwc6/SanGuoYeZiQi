using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 手牌管理器
/// </summary>
public partial class HandManager : HBoxContainer, IDeck
{
    [Export]
    public int MaxCardNum = 10;
    [Export]
    private OwnerType ownerType;
    [Export]
    private Marker2D markPosition;

    public List<Card> cards = [];

    private GameEvent gameEvent;   

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        // 连接卡牌信号
        gameEvent.Connect(GameEvent.SignalName.CardClicked, Callable.From<Card>(OnCardClicked));
    }

    private void OnCardClicked(Card card)
    {
        if (ownerType != card.ownerType) return;
        // 同时只允许一张卡被选中
        foreach (var item in cards)
        {
            if (item == card) continue;
            item.ChangedToState(BaseState.State.Normal);
        }
    }

    /// <summary>
    /// 获取手牌和最大手牌数的差值
    /// </summary>
    /// <returns></returns>
    public int GetCardMinus()
    {
        return cards.Count - MaxCardNum;
    }

    /// <summary>
    /// 设置禁用
    /// </summary>
    /// <param name="value"></param>
    public void SetDisable(bool value)
    {
        if (cards.Count == 0) return;
        foreach (var item in cards)
        {
            item.disabled = value;
        }
    }

    /// <summary>
    /// 重置手牌状态
    /// </summary>
    public void ResetHandStatus()
    {
        if (cards.Count == 0) return;
        foreach (var item in cards)
        {
            item.ChangedToState(BaseState.State.Normal);
        }
    }

    /// <summary>
    /// 添加卡牌到手牌
    /// </summary>
    /// <param name="card"></param>
    public void AddCard(Card card)
    {
        if (ownerType != card.ownerType) return;
        if (card == null) return;
        AddChild(card);
        cards.Add(card);
        if (ownerType == OwnerType.Enemy)
        {
            card.SetBackShow(true);
        }
        else
        {
            card.SetBackShow(false);
        }
        card.ChangedToState(BaseState.State.Normal);
    }

    /// <summary>
    /// 移除指定手牌
    /// </summary>
    /// <param name="card"></param>
    public void RemoveCard(Card card)
    {
        if (ownerType != card.ownerType) return;
        cards.Remove(card);
        RemoveChild(card);
    }

    /// <summary>
    /// 随机获取一张手牌
    /// </summary>
    /// <returns></returns>
    public Card GetRandomCard()
    {
        if (cards.Count == 0) return null;
        Random random = new Random();
        int index = random.Next(cards.Count);
        var card = cards[index];
        return card;
    }

    /// <summary>
    /// 清空手牌
    /// </summary>
    public void Clear()
    {
        foreach (var item in GetChildren())
        {
            item.QueueFree();
        }
        cards.Clear();
    }

    /// <summary>
    /// 获取描点
    /// </summary>
    /// <returns></returns>
    public Vector2 GetMarkPosition()
    {
        return markPosition.GlobalPosition;
    }

    /// <summary>
    /// 获取卡牌数据
    /// </summary>
    /// <returns></returns>
    public List<CardData> GetCardDatas()
    {
        var list = new List<CardData>();
        foreach (var item in cards)
        {
            list.Add(item.cardData);
        }
        return list;
    }

    /// <summary>
    /// 获取卡牌
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Card GetCard(string name)
    {
        var card = cards.FirstOrDefault(x => x.cardData.Name == name);
        return card;
    }

    /// <summary>
    /// 重写ToString方法
    /// </summary>
    public override string ToString()
    {
        var names = new List<string>();
        foreach (var item in cards)
        {
            names.Add(item.cardData.Name);
        }

        return string.Join(",", names);
    }
}
