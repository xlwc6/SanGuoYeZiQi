using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 卡牌选择器
/// </summary>
public partial class SelectorGridPanel : ScrollContainer
{
    /// <summary>
    /// 选择完成事件
    /// </summary>
    [Signal]
    public delegate void SelectDoneEventHandler(bool flag);

    /// <summary>
    /// 最大选择数
    /// </summary>
    [Export(PropertyHint.Range,"1,99")]
    public int MaxSelect = 1;
    /// <summary>
    /// 最大显示列数
    /// </summary>
    [Export(PropertyHint.Range, "1,9")]
    private int MaxColum = 3;

    private GridContainer grid;

    private string cardSlotScene = "res://ui/card_slot.tscn";
    private List<CardData> allCards = [];// 全部的卡牌
    private List<CardData> selectedCards = [];// 选择的卡牌

    public override void _Ready()
    {
        grid = GetNode<GridContainer>("卡牌列表");

        grid.Columns = MaxColum;
    }

    /// <summary>
    /// 选择的卡牌
    /// </summary>
    public CardData[] SelectedCards
    {
        get { return selectedCards.ToArray(); }
    }

    /// <summary>
    /// 清空现有数据
    /// </summary>
    public void Clear()
    {
        foreach (var item in grid.GetChildren())
        {
            item.QueueFree();
        }
        allCards.Clear();
        selectedCards.Clear();
    }

    /// <summary>
    /// 设置卡牌列表
    /// </summary>
    /// <param name="deck"></param>
    public void SetCards(IDeck deck)
    {
        allCards = deck.GetCardDatas();
        if (allCards.Count > 0)
        {
            foreach (var item in allCards)
            {
                var cardSlot = ResourceLoader.Load<PackedScene>(cardSlotScene).Instantiate() as CardSlot;
                cardSlot.cardData = item;
                grid.AddChild(cardSlot);
                cardSlot.Connect(CardSlot.SignalName.Selected, Callable.From<bool, CardData>(CardSlot_Selected));
            }
        }
    }

    /// <summary>
    /// 随机选择卡牌
    /// </summary>
    public void RandomSelectCard()
    {
        if (selectedCards.Count < MaxSelect)
        {
            int mins = MaxSelect - selectedCards.Count;
            for (int i = 0; i < mins; i++)
            {
                var canSelecteds = allCards.Except(selectedCards).ToList();
                // 获取一个随机数
                int index = GD.RandRange(0, canSelecteds.Count - 1);
                selectedCards.Add(canSelecteds[index]);
            }
        }
    }

    private void CardSlot_Selected(bool flag, CardData cardData)
    {
        if (flag)
        {
            selectedCards.Add(cardData);
        }
        else
        {
            selectedCards.Remove(cardData);
        }
        // 如果选择已满，其他全部设置禁用
        if (selectedCards.Count == MaxSelect)
        {
            SetCardSlotDisable(true);
            EmitSignal(SignalName.SelectDone, true);
        }
        else
        {
            SetCardSlotDisable(false);
            EmitSignal(SignalName.SelectDone, false);
        }
    }

    private void SetCardSlotDisable(bool flag)
    {
        foreach (var item in grid.GetChildren().Cast<CardSlot>())
        {
            if (selectedCards.Contains(item.cardData)) continue;
            item.SetDisabled(flag);
        }
    }
}
