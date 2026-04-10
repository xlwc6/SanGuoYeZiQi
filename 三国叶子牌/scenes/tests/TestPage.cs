using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 测试页
/// </summary>
public partial class TestPage : Node
{
    private const float takeDuration = 0.5f; // 模拟抽卡的耗时
    private const float moveDuration = 1.0f; // 移动卡牌的耗时
    private const int handCount = 10; // 手牌数量

    private GameEvent gameEvent;
    private AiCompeonent enemyAI;
    private HandManager enemyHandManager;
    private CardPile enemyCardPile;
    private HandManager playerHandManager;
    private CardPile playerCardPile;
    private Button sBtn;
    private Button bBtn;
    private Button oBtn;
    private CardDiscardScene disCardLayer;
    private GameStartScene startLayer;

    private WeightedTable<string> cardTable = new WeightedTable<string>("卡牌池");
    private string cardScene = "res://ui/card.tscn";

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        enemyAI = GetNode<AiCompeonent>("敌人AI");
        enemyHandManager = GetTree().GetFirstNodeInGroup("enemy_deck") as HandManager;
        enemyCardPile = GetTree().GetFirstNodeInGroup("enemy_discard_deck") as CardPile;
        playerHandManager = GetTree().GetFirstNodeInGroup("player_deck") as HandManager;
        playerCardPile = GetTree().GetFirstNodeInGroup("player_discard_deck") as CardPile;
        sBtn = GetNode<Button>("%发牌");
        bBtn = GetNode<Button>("%弃牌");
        oBtn = GetNode<Button>("%弹出");
        disCardLayer = GetNode<CardDiscardScene>("弃牌界面");
        startLayer = GetNode<GameStartScene>("游戏开始");

        gameEvent.Connect(GameEvent.SignalName.CardDiscarded, Callable.From<int, string[]>(OnCardDisCarded));
        sBtn.Pressed += SBtn_Pressed;
        bBtn.Pressed += BBtn_Pressed;
        oBtn.Pressed += OBtn_Pressed;

        InitCardTable();
        //ShowStartPanel();
    }

    private void ShowStartPanel()
    {
        var enemyInfo = GetNode<EnemyCanvasLayer>("敌方界面").EnemyInfo;
        var playerInfo = GetNode<PlayerCanvasLayer>("玩家界面").PlayerInfo;

        startLayer.ShowStart(enemyInfo, playerInfo);
    }

    private async void OnCardDisCarded(int owner, string[] cards)
    {
        List<Task> tasks = new List<Task>();
        // 模拟抽出卡牌然后移入弃牌堆
        foreach (var item in cards)
        {
            var card = playerHandManager.GetCard(item);
            // 向上移动60像素然后在移入弃牌堆
            var transferPos = card.GlobalPosition + new Vector2(0, -200);
            // 添加到任务列表，一起执行并等待
            tasks.Add(FollowTargetPostion(playerHandManager, playerCardPile, card, transferPos));
        }
        await Task.WhenAll(tasks);
    }

    private void BBtn_Pressed()
    {
        int needSelect = playerHandManager.cards.Count - playerHandManager.MaxCardNum;
        if (needSelect > 0)
        {
            GD.Print($"牌多了，丢{needSelect}张");
            disCardLayer.ShowPanelByDeck(playerHandManager, needSelect);
        }
        else
        {
            GD.Print("牌没有问题");
        }
    }

    private void SBtn_Pressed()
    {
        HandOutCards();
    }

    private void OBtn_Pressed()
    {
        //ShowStartPanel();
        var detail = enemyAI.GetPersonality();
        GD.Print(detail);
        enemyAI.PlayCard();
    }

    private void InitCardTable()
    {
        cardTable.Clear();
        cardTable.Add("res://resources/card_data/qun/刘协.tres", 1);
        cardTable.Add("res://resources/card_data/qun/吕布.tres", 1);
        cardTable.Add("res://resources/card_data/qun/张角.tres", 1);
        cardTable.Add("res://resources/card_data/qun/文丑.tres", 1);
        cardTable.Add("res://resources/card_data/qun/袁绍.tres", 1);
        cardTable.Add("res://resources/card_data/qun/貂蝉.tres", 1);
        cardTable.Add("res://resources/card_data/qun/颜良.tres", 1);
        cardTable.Add("res://resources/card_data/shu/关羽.tres", 1);
        cardTable.Add("res://resources/card_data/shu/刘备.tres", 1);
        cardTable.Add("res://resources/card_data/shu/姜维.tres", 1);
        cardTable.Add("res://resources/card_data/shu/张飞.tres", 1);
        cardTable.Add("res://resources/card_data/shu/法正.tres", 1);
        cardTable.Add("res://resources/card_data/shu/诸葛亮.tres", 1);
        cardTable.Add("res://resources/card_data/shu/赵云.tres", 1);
        cardTable.Add("res://resources/card_data/shu/马超.tres", 1);
        cardTable.Add("res://resources/card_data/shu/黄忠.tres", 1);
        cardTable.Add("res://resources/card_data/wei/典韦.tres", 1);
        cardTable.Add("res://resources/card_data/wei/司马懿.tres", 1);
        cardTable.Add("res://resources/card_data/wei/夏侯渊.tres", 1);
        cardTable.Add("res://resources/card_data/wei/张辽.tres", 1);
        cardTable.Add("res://resources/card_data/wei/曹操.tres", 1);
        cardTable.Add("res://resources/card_data/wei/许褚.tres", 1);
        cardTable.Add("res://resources/card_data/wei/郭嘉.tres", 1);
        cardTable.Add("res://resources/card_data/wu/周泰.tres", 1);
        cardTable.Add("res://resources/card_data/wu/周瑜.tres", 1);
        cardTable.Add("res://resources/card_data/wu/太史慈.tres", 1);
        cardTable.Add("res://resources/card_data/wu/孙权.tres", 1);
        cardTable.Add("res://resources/card_data/wu/甘宁.tres", 1);
        cardTable.Add("res://resources/card_data/wu/陆逊.tres", 1);
        cardTable.Add("res://resources/card_data/wu/鲁肃.tres", 1);
    }

    private void HandOutCards()
    {
        if (cardTable.Count == 0) return;
        var tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        for (int i = 0; i < handCount; i++)
        {
            tween.TweenCallback(Callable.From(() => {
                // 给敌人手牌
                GiveToCard(enemyHandManager, OwnerType.Enemy);
            }));
            tween.TweenCallback(Callable.From(() => {
                // 给玩家手牌
                GiveToCard(playerHandManager, OwnerType.Player);
            }));
            tween.TweenInterval(0.25f);
        }
    }

    private void GiveToCard(HandManager manager, OwnerType ownerType)
    {
        //if (manager.HandFull()) return;
        var cardRes = cardTable.RandomPopOne();
        var card = ResourceLoader.Load<PackedScene>(cardScene).Instantiate() as Card;
        card.cardData = ResourceLoader.Load<CardData>(cardRes);
        card.ownerType = ownerType;
        card.disabled = true;
        manager.AddCard(card);
    }

    private async Task FollowTargetPostion(Control from, Control to, Card card, Vector2 transferPos)
    {
        card.ZIndex = 1;
        // 获取移动到的描点位置
        var targetPos = to.GlobalPosition;
        if (to is IDeck)
        {
            targetPos = (to as IDeck).GetMarkPosition();
        }
        // 移动卡牌到目标位置并修改父级
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "global_position", transferPos, takeDuration);
        tween.TweenProperty(card, "global_position", targetPos, moveDuration);
        tween.TweenCallback(Callable.From(() =>
        {
            // 移除原来父级
            if (from is IDeck deckFrom)
            {
                deckFrom.RemoveCard(card);
            }
            else
            {
                from.RemoveChild(card);
            }
            // 添加到新的父级
            if (to is IDeck deckTo)
            {
                deckTo.AddCard(card);
            }
            else
            {
                to.AddChild(card);
            }
        }));
        await ToSignal(tween, "finished");
        card.ZIndex = 0;
    }
}
