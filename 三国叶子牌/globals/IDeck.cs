using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 牌堆类
/// </summary>
public interface IDeck
{
    public void AddCard(Card card);
    public void RemoveCard(Card card);
    public void Clear();
    public Vector2 GetMarkPosition();
    public List<CardData> GetCardDatas();
}
