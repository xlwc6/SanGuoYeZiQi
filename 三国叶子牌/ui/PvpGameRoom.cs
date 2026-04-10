using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public partial class PvpGameRoom : Control
{
    private const int roundTick = 30; // 每回合的时间，秒
    private const int startCardNum = 10; // 初始化手牌数量
    private const float takeDuration = 0.5f; // 模拟抽卡的耗时
    private const float moveDuration = 1.0f; // 移动卡牌的耗时
    private const int damage = 1; // 每次受到的伤害
    private const string lobbyScene = "res://ui/lobby.tscn";
    private const string cardScene = "res://ui/card.tscn"; // 卡牌场景
    private const string skillHMSScene = "res://scenes/special_effects/half_moon_slash.tscn"; // 半月斩技能

    private GameEvent gameEvent;
    private SoundManager soundManager;
    private HandManager enemyHandManager;
    private HandManager playerHandManager;
    private CardPile enemyCardPile;
    private CardPile playerCardPile;
    private PanelContainer battleLayer;
    private VBoxContainer turnLayer;
    private PanelContainer leftCard;
    private PanelContainer rightCard;
    private Label roundLabel;
    private Timer roundTimer;
    private Label timerLabel;
    private GameOverScene endLayer;
    private CanvasLayer skillLayer;
    private Marker2D enemyPos;
    private Marker2D playerPos;
    private CardDiscardScene disCardLayer;
    private PlayerCanvasLayer playerCanvasLayer;
    private EnemyCanvasLayer enemyCanvasLayer;
    private WaitPage waitPage;
    private Timer readyTimer;

    // 游戏回合数据
    private WeightedTable<string> cardTable = new("卡牌池");
    private bool isGameOver = false; // 游戏是否结束
    private int currentRound = 0; // 当前回合数
    private int currentRoundTick = 0; // 当前回合耗时
    private List<BuffInfo> buffs = []; // buff数组
    private int forgingCardPeerId = -1; // 正在弃牌的玩家ID
    private bool skipRound = false; // 掉过回合
    private Dictionary<int, bool> peerPlayeds = []; // 记录哪些玩家已经出牌，这个数组仅服务器可处理
    private List<int> readyPeerIds = []; // 已准备的玩家

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        battleLayer = GetNode<PanelContainer>("%战斗面板");
        turnLayer = GetNode<VBoxContainer>("%回合信息");
        leftCard = GetNode<PanelContainer>("%左卡牌");
        rightCard = GetNode<PanelContainer>("%右卡牌");
        roundLabel = GetNode<Label>("%回合数");
        roundTimer = GetNode<Timer>("%回合计时器");
        timerLabel = GetNode<Label>("%倒计时");
        endLayer = GetNode<GameOverScene>("%结束界面");
        skillLayer = GetNode<CanvasLayer>("技能层");
        enemyPos = GetNode<Marker2D>("%敌方位置");
        playerPos = GetNode<Marker2D>("%玩家位置");
        disCardLayer = GetNode<CardDiscardScene>("%弃牌界面");
        playerCanvasLayer = GetNode<PlayerCanvasLayer>("玩家界面");
        enemyCanvasLayer = GetNode<EnemyCanvasLayer>("敌方界面");
        waitPage = GetNode<WaitPage>("%等待页面");
        readyTimer = GetNode<Timer>("准备就绪计时器");

        playerHandManager = GetTree().GetFirstNodeInGroup("player_deck") as HandManager;
        playerCardPile = GetTree().GetFirstNodeInGroup("player_discard_deck") as CardPile;
        enemyHandManager = GetTree().GetFirstNodeInGroup("enemy_deck") as HandManager;
        enemyCardPile = GetTree().GetFirstNodeInGroup("enemy_discard_deck") as CardPile;

        // 全局游戏事件
        gameEvent.Connect(GameEvent.SignalName.CardPlayed, Callable.From<Card>(OnCardPlayed));
        gameEvent.Connect(GameEvent.SignalName.Death, Callable.From<int>(OnDeathed));
        gameEvent.Connect(GameEvent.SignalName.CardDiscarded, Callable.From<int, string[]>(OnCardDisCarded));
        gameEvent.Connect(GameEvent.SignalName.ContinueGame, Callable.From<int>(OnContinueGame));
        gameEvent.Connect(GameEvent.SignalName.QuitGameRequested, Callable.From(OnQuitGameRequested));

        roundTimer.Timeout += RoundTimer_Timeout;

        // 初始化参数
        InitCardTable();
        InitDeckPanel();

        soundManager.PlayBGM("战斗");

        // 多人同步相关
        Multiplayer.ServerDisconnected += Multiplayer_ServerDisconnected;
        // 服务器操作
        if (IsMultiplayerAuthority())
        {
            Multiplayer.PeerConnected += Multiplayer_PeerConnected;
            Multiplayer.PeerDisconnected += Multiplayer_PeerDisconnected;
            readyTimer.Timeout += ReadyTimer_Timeout;

            // 等待加入
            waitPage.SetMessage("请等待玩家加入房间");
            waitPage.Visible = true;

            readyPeerIds.Add(Multiplayer.GetUniqueId());
            readyTimer.Start();
        }

        // 客户端操作
        if (!IsMultiplayerAuthority())
        {
            readyPeerIds.Add(Multiplayer.GetUniqueId());

            // 告诉服务器，我的玩家数据
            RpcId(MultiplayerPeer.TargetPeerServer, MethodName.TakePeerReady, new Godot.Collections.Dictionary<string, Variant>
            {
                ["PID"] = playerCanvasLayer.PlayerInfo.PID,
                ["Name"] = playerCanvasLayer.PlayerInfo.Name,
                ["Avatar"] = playerCanvasLayer.PlayerInfo.Avatar
            });
        }
    }

    #region 多人联机部分

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TakePeerReady(Variant data)
    {
        readyPeerIds.Add(Multiplayer.GetRemoteSenderId());
        // 实例化玩家
        var playerData = data.AsGodotDictionary<string, Variant>();
        var playerInfo = new PlayerInfo
        {
            PID = playerData["PID"].AsString(),
            Name = playerData["Name"].AsString(),
            Avatar = playerData["Avatar"].AsString(),
        };
        enemyCanvasLayer.UpdatePlatInfo(playerInfo);

        enemyCanvasLayer.Visible = true;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void GiveToCard(string cardRes, int ownerType)
    {
        HandManager manager;
        bool isServer = IsMultiplayerAuthority();
        OwnerType owner;
        if (ownerType == (int)OwnerType.Player)
        {
            manager = isServer ? playerHandManager : enemyHandManager;
            owner = isServer ? OwnerType.Player : OwnerType.Enemy;
        }
        else
        {
            manager = isServer ? enemyHandManager : playerHandManager;
            owner = isServer ? OwnerType.Enemy : OwnerType.Player;
        }
        if (manager.GetCardMinus() >= 0) return;
        var card = ResourceLoader.Load<PackedScene>(cardScene).Instantiate() as Card;
        card.cardData = ResourceLoader.Load<CardData>(cardRes);
        card.ownerType = owner;
        card.disabled = true;
        manager.AddCard(card);
        soundManager.PlaySFX("发牌");
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SynchronizeRound(Godot.Collections.Dictionary<string, Variant> data)
    {
        // 更新同步过来的信息
        var isRunning = data["RoundTimerIsRunning"].AsBool();
        var isShow = data["RoundTimerIsShow"].AsBool();
        var count = data["RoundCount"].AsInt32();

        if (!isRunning)
        {
            roundTimer.Stop();
        }
        timerLabel.Visible = isShow;
        if (currentRound != count)
        {
            currentRound = count;
            NewRound();
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private async void StartBattle()
    {
        //GD.Print(IsMultiplayerAuthority() ? "主机开始决斗" : "客机开始决斗");
        // 显示回合信息
        await ShowTurnInfoAddBattlePanel(true);
        // 这里等待1秒，避免上个动画未结束
        await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
        // 禁止播放动画时能交互，并且把卡牌状态还原
        playerHandManager.SetDisable(true);
        playerHandManager.ResetHandStatus();
        // 获取两个出牌区的卡牌信息，然后进行对决
        var cardEnemy = leftCard.GetChild(0) as Card;
        var cardPlayer = rightCard.GetChild(0) as Card;
        cardEnemy.SetBackShow(false);
        // 开始对决
        await BattleCard(cardEnemy, cardPlayer);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayCardHandle(string cardName)
    {
        // 由于卡牌场景都是在各自主场景实例化，节点ID肯定不一样，所以需要通过卡牌名称找到自己对应的那个节点
        int sendPeerId = Multiplayer.GetRemoteSenderId();
        int localPeerId = Multiplayer.GetUniqueId();
        //GD.Print($"发送人：{sendPeerId}，接收人：{localPeerId}，卡牌：{cardName}");
        if (sendPeerId == localPeerId) // 发送人和本地peerID一致，表示自己出牌
        {
            // 如果已经出牌，就不处理
            if(rightCard.GetChildCount() == 0)
            {
                var card = playerHandManager.GetCard(cardName);
                _ = FollowTargetPostion(playerHandManager, rightCard, card);
            }
        }
        else // 否则，肯定是对方，多人的话，无非多几次判断
        {
            // 如果已经出牌，就不处理
            if (leftCard.GetChildCount() == 0)
            {
                var card = enemyHandManager.GetCard(cardName);
                _ = FollowTargetPostion(enemyHandManager, leftCard, card);
            }
        }

        // 仅服务器处理，因为服务器控制是否下一回合
        if (IsMultiplayerAuthority()&& peerPlayeds.ContainsKey(sendPeerId))
        {
            peerPlayeds[sendPeerId] = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DisCardHandle(string[] cardNames)
    {
        // 告诉其他连接端，我丢牌了
        gameEvent.EmitSignal(GameEvent.SignalName.CardDiscarded, (int)OwnerType.Enemy, cardNames);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestDisCard()
    {
        if (forgingCardPeerId > -1) return;

        // 由服务器处理
        Rpc(MethodName.DisCard, Multiplayer.GetRemoteSenderId());
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DisCard(int peerId)
    {
        forgingCardPeerId = peerId;

        if (forgingCardPeerId == Multiplayer.GetUniqueId())
        {
            int minus = playerHandManager.GetCardMinus();
            disCardLayer.ShowPanelByDeck(playerHandManager, minus);
        }
        else
        {
            waitPage.SetMessage("等待对方弃牌中");
            waitPage.Visible = true;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestDisCardCompleted()
    {
        if (forgingCardPeerId != Multiplayer.GetRemoteSenderId()) return;

        Rpc(MethodName.DisCardCompleted);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void DisCardCompleted()
    {
        if (forgingCardPeerId != Multiplayer.GetUniqueId())
        {
            waitPage.Visible = false;
        }
        forgingCardPeerId = -1;

        NetxRound();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void HideWaitPage()
    {
        waitPage.Visible = false;
    }

    #endregion

    #region 事件回调

    private void RoundTimer_Timeout()
    {
        // 每次更新倒计时显示和判断是否都已出牌
        currentRoundTick++;
        int minus = Mathf.Clamp(roundTick - currentRoundTick, 0, roundTick);
        if (minus > 5)
        {
            timerLabel.Text = minus.ToString();
            timerLabel.Set("theme_override_colors/font_color", "#ffffff");
        }
        else
        {
            timerLabel.Text = minus.ToString();
            timerLabel.Set("theme_override_colors/font_color", "#eb2d5d");
        }

        // 都出牌了，就结束这回合进入战斗
        if (CheckPlayCard() == 3)
        {
            RequsetStartBattle();
            return;
        }
        // 超时随机出牌由本地判断并出牌比较好，避免影响原本逻辑
        if (currentRoundTick >= roundTick)
        {
            if (CheckPlayCard() < 2)
            {
                // 玩家行动超时
                var card = playerHandManager.GetRandomCard();
                card.ChangedToState(BaseState.State.Played);

                // 如果是客服端就停止本地计时器，避免延时引起重复出牌
                if (!IsMultiplayerAuthority())
                {
                    roundTimer.Stop();
                }
            }
        }
    }

    private void OnCardPlayed(Card card)
    {
        // 玩家对玩家，当打出牌后，只需要调用本地多人方法就行
        Rpc(MethodName.PlayCardHandle, card.cardData.Name);
    }

    private void OnDeathed(int ownerType)
    {
        readyPeerIds.Clear();

        var winer = ownerType == 1 ? "敌人" : "玩家";
        isGameOver = true;
        endLayer.SetInfo($"游戏结束，{winer}胜利！");
        endLayer.Visible = true;
        // 切换背景
        if (winer == "玩家")
        {
            soundManager.PlayBGM("胜利");
        }
        else
        {
            soundManager.PlayBGM("失败");
        }
    }

    private async void OnCardDisCarded(int owner, string[] cards)
    {
        List<Task> tasks = new List<Task>();
        if ((OwnerType)owner == OwnerType.Player)
        {
            // 模拟抽出卡牌然后移入弃牌堆
            foreach (var item in cards)
            {
                var card = playerHandManager.GetCard(item);
                var transferPos = card.GlobalPosition + new Vector2(0, -200);
                // 添加到任务列表，一起执行并等待
                tasks.Add(FollowTargetPostion(playerHandManager, playerCardPile, card, transferPos));
            }

            // 通知丢牌
            Rpc(MethodName.DisCardHandle, cards);
        }
        else if ((OwnerType)owner == OwnerType.Enemy)
        {
            // 模拟抽出卡牌然后移入弃牌堆
            foreach (var item in cards)
            {
                var card = enemyHandManager.GetCard(item);
                var transferPos = card.GlobalPosition + new Vector2(0, 200);
                tasks.Add(FollowTargetPostion(enemyHandManager, enemyCardPile, card, transferPos));
            }
        }
        await Task.WhenAll(tasks);

        // 通知弃牌结束
        RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RequestDisCardCompleted);
    }

    private void OnContinueGame(int peerId)
    {
        
        // 检测是否都已准备，否则自己显示等待中
        if (peerId == Multiplayer.GetUniqueId())
        {
            NewGame();

            waitPage.SetMessage("请等待玩家加入房间");
            waitPage.Visible = !CheckAllPeersReady();
        }

        // 服务器处理
        if (IsMultiplayerAuthority())
        {
            readyPeerIds.Add(peerId);
            GD.Print("已准备玩家：" + string.Join(",", readyPeerIds));

            // 如果发起人是服务器，就等待玩家连接
            if (peerId == MultiplayerPeer.TargetPeerServer)
            {
                readyTimer.Start();
            }
        }
    }

    private void OnQuitGameRequested()
    {
        EndGame();
    }

    private void Multiplayer_ServerDisconnected()
    {
        roundTimer.Stop();

        EndGame();
    }

    private void Multiplayer_PeerConnected(long id)
    {
        if (!IsMultiplayerAuthority()) return;

        AppGlobalData.ConnectNum += 1;
        // 告诉客户端，服务器的玩家数据
        RpcId(id, MethodName.TakePeerReady, new Godot.Collections.Dictionary<string, Variant>
        {
            ["PID"] = playerCanvasLayer.PlayerInfo.PID,
            ["Name"] = playerCanvasLayer.PlayerInfo.Name,
            ["Avatar"] = playerCanvasLayer.PlayerInfo.Avatar
        });
    }

    private void Multiplayer_PeerDisconnected(long id)
    {
        if (!IsMultiplayerAuthority()) return;

        AppGlobalData.ConnectNum -= 1;
        roundTimer.Stop();
        if (id != Multiplayer.GetUniqueId())
        {
            gameEvent.EmitSignal(GameEvent.SignalName.Death, (int)OwnerType.Enemy);
        }
    }

    private void ReadyTimer_Timeout()
    {
        // 这个回调仅服务器处理
        if (CheckAllPeersReady())
        {
            readyTimer.Stop();

            Rpc(MethodName.HideWaitPage);

            // 开始抽卡
            HandOutCards();
        }
    }

    #endregion

    #region 私有方法

    private void InitDeckPanel()
    {
        endLayer.Visible = false;
        leftCard.SelfModulate = leftCard.SelfModulate with { A = 0 };
        rightCard.SelfModulate = rightCard.SelfModulate with { A = 0 };
        _ = ShowTurnInfoAddBattlePanel(false);
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

    private void NewGame()
    {
        isGameOver = false;
        currentRound = 0;
        currentRoundTick = 0;
        foreach (var item in leftCard.GetChildren())
        {
            item.QueueFree();
        }
        foreach (var item in rightCard.GetChildren())
        {
            item.QueueFree();
        }
        playerHandManager.Clear();
        enemyHandManager.Clear();
        playerCardPile.Clear();
        enemyCardPile.Clear();

        InitCardTable();
        InitDeckPanel();
    }

    private void NewRound()
    {
        // 服务器初始化出牌
        if(IsMultiplayerAuthority())
        {
            foreach (int peerId in GetAllPeers())
            {
                if (!peerPlayeds.TryAdd(peerId, false))
                {
                    peerPlayeds[peerId] = false;
                }
            }
            //GD.Print("需要准备玩家：" + string.Join(",", peerPlayeds.Keys));
        }

        // 本地是否需要跳过当前回合
        if (skipRound)
        {
            // 通知已经出牌
            var cardPlayer = rightCard.GetChild(0) as Card;
            Rpc(MethodName.PlayCardHandle, cardPlayer.cardData.Name);

            skipRound = false;
        }

        // 回合信息
        roundLabel.Text = $"第{NumberToChineseOptimized(currentRound)}局";
        currentRoundTick = 0;
        timerLabel.Text = roundTick.ToString();
        timerLabel.Set("theme_override_colors/font_color", "#ffffff");
        timerLabel.Visible = true;
        roundTimer.Start();
        gameEvent.EmitSignal(GameEvent.SignalName.TurnBegin);
        playerHandManager.SetDisable(false);
    }

    // 检查谁没出牌，没出0，左边出了1，右边出了2，都出了3
    private int CheckPlayCard()
    {
        int leftPlayed = leftCard.GetChildCount() > 0 ? 1 : 0;
        int rightPlayed = rightCard.GetChildCount() > 0 ? 2 : 0;
        return leftPlayed + rightPlayed;
    }

    private async Task BattleCard(Card cardEnemy, Card cardPlayer)
    {
        // 先把卡牌的增量还原成0，然后再判断特殊的回合的延续BUFF
        cardEnemy.cardData.AdditionalPower = 0;
        cardEnemy.cardData.AdditionalWisdom = 0;
        cardPlayer.cardData.AdditionalPower = 0;
        cardPlayer.cardData.AdditionalWisdom = 0;
        if (buffs.Count > 0)
        {
            foreach (var bf in buffs)
            {
                if (bf.Target == OwnerType.Player)
                {
                    cardPlayer.cardData.AdditionalPower += bf.AdditionalPower;
                    cardPlayer.cardData.AdditionalWisdom += bf.AdditionalWisdom;
                }
                else if (bf.Target == OwnerType.Enemy)
                {
                    cardEnemy.cardData.AdditionalPower += bf.AdditionalPower;
                    cardEnemy.cardData.AdditionalWisdom += bf.AdditionalWisdom;
                }
            }
            buffs.Clear();
        }
        if (cardEnemy.cardData.Wisdom > cardPlayer.cardData.Wisdom)
        {
            // 播放攻击动画
            await cardEnemy.PlayAniAsync("攻击");
            await cardPlayer.PlayAniAsync("攻击");
        }
        else
        {
            // 播放攻击动画
            await cardPlayer.PlayAniAsync("攻击");
            await cardEnemy.PlayAniAsync("攻击");
        }
        // 比较卡牌
        var score = cardPlayer.cardData.Compare(cardEnemy.cardData);
        // 判断战后技能，否则胜者赢掉败者本轮牌，胜者本轮的牌弃牌。
        if (score == 0) // 战平
        {
            // 丢弃卡牌
            await FollowTargetPostion(leftCard, enemyCardPile, cardEnemy);
            await FollowTargetPostion(rightCard, playerCardPile, cardPlayer);
            // 触发伤害
            Task task1 = CastSkillAsync(skillHMSScene, playerPos.GlobalPosition, enemyPos.GlobalPosition);
            Task task2 = CastSkillAsync(skillHMSScene, enemyPos.GlobalPosition, playerPos.GlobalPosition);
            await Task.WhenAll(task1, task2);
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Enemy, damage);
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Player, damage);
        }
        else if (score > 0) // 玩家赢
        {
            // 战后技能判断
            if (cardPlayer.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardPlayer, true);
            }
            if (cardEnemy.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardEnemy, false);
            }
            // 丢弃卡牌和获取对方卡牌
            if (!cardEnemy.IsQueuedForDeletion() && cardEnemy.GetParent() == leftCard)
            {
                cardEnemy.ownerType = OwnerType.Player;
                await FollowTargetPostion(leftCard, playerHandManager, cardEnemy);
            }
            if (!cardPlayer.IsQueuedForDeletion() && cardPlayer.GetParent() == rightCard)
            {
                if (cardPlayer.cardData.Skill != "武圣")
                {
                    await FollowTargetPostion(rightCard, playerCardPile, cardPlayer);
                }
            }
            // 触发伤害
            await CastSkillAsync(skillHMSScene, playerPos.GlobalPosition, enemyPos.GlobalPosition);
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Enemy, damage);
        }
        else // 对面赢
        {
            // 战后技能判断
            if (cardEnemy.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardEnemy, true);
            }
            if (cardPlayer.cardData.Type == CardData.SkillType.After)
            {
                await Aftermath(cardPlayer, false);
            }
            // 丢弃卡牌和获取对方卡牌
            if (!cardPlayer.IsQueuedForDeletion() && cardPlayer.GetParent() == rightCard)
            {
                cardPlayer.ownerType = OwnerType.Enemy;
                await FollowTargetPostion(rightCard, enemyHandManager, cardPlayer);
            }
            if (!cardEnemy.IsQueuedForDeletion() && cardEnemy.GetParent() == leftCard)
            {
                if (cardEnemy.cardData.Skill != "武圣")
                {
                    await FollowTargetPostion(leftCard, enemyCardPile, cardEnemy);
                }
            }
            // 触发伤害
            await CastSkillAsync(skillHMSScene, enemyPos.GlobalPosition, playerPos.GlobalPosition);
            gameEvent.EmitSignal(GameEvent.SignalName.BeHurt, (int)OwnerType.Player, damage);
        }
        await ShowTurnInfoAddBattlePanel(false);
        // 如果游戏未结束
        if (!isGameOver)
        {
            // 每回合结束检查自己手牌，多出的要丢弃，然后在丢牌回调进入下回合
            if (playerHandManager.GetCardMinus() > 0)
            {
                // 并让服务器通知弃牌
                RpcId(MultiplayerPeer.TargetPeerServer, MethodName.RequestDisCard);
            }
            else
            {
                // 如果对方手牌多了，就需要等待对面弃牌
                if (enemyHandManager.GetCardMinus() > 0) return;

                // 直接进入下回合
                NetxRound();
            }
        }
    }

    private async Task Aftermath(Card card, bool isWin)
    {
        // 处理战后技能的工作
        if (card.cardData.Skill == "举义" && !isWin)
        {
            // 直接进我方弃牌堆
            if (card.ownerType == OwnerType.Player)
            {
                await FollowTargetPostion(rightCard, playerCardPile, card);
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                await FollowTargetPostion(leftCard, enemyCardPile, card);
            }
        }
        if (card.cardData.Skill == "常胜" && isWin)
        {
            // 回到我方手牌
            if (card.ownerType == OwnerType.Player)
            {
                await FollowTargetPostion(rightCard, playerHandManager, card);
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                await FollowTargetPostion(leftCard, enemyHandManager, card);
            }
        }
        if (card.cardData.Skill == "武圣")
        {
            // 我方跳过出牌
            if (card.ownerType == OwnerType.Player)
            {
                if(isWin)
                {
                    gameEvent.EmitSignal(GameEvent.SignalName.TurnSkipNext);

                    // 跳过当前回合
                    skipRound = true;
                }
                else
                {
                    // 从本局游戏移除卡牌
                    card.QueueFree();
                }
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                if (!isWin)
                {
                    // 从本局游戏移除卡牌
                    card.QueueFree();
                }
            }
        }
        if (card.cardData.Skill == "遗计" && !isWin)
        {
            // 给我方下个牌加Buff
            buffs.Add(new BuffInfo
            {
                Target = card.ownerType,
                AdditionalPower = 20
            });
        }
        if (card.cardData.Skill == "火神" && isWin)
        {
            // 随机丢弃对方一张手牌
            if (card.ownerType == OwnerType.Player)
            {
                var discardCard = enemyHandManager.GetRandomCard();
                if (discardCard != null)
                {
                    await AnalogTakeCardToPostion(enemyHandManager, enemyCardPile, discardCard);
                }
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                var discardCard = playerHandManager.GetRandomCard();
                if (discardCard != null)
                {
                    await AnalogTakeCardToPostion(playerHandManager, playerCardPile, discardCard);
                }
            }
        }
        if (card.cardData.Skill == "虎臣" && !isWin)
        {
            // 随机抽取一张我方弃牌堆里的吴国牌
            if (card.ownerType == OwnerType.Player)
            {
                var newCard = playerCardPile.GetRandomCard(CardData.CampType.Wu);
                if (newCard != null)
                {
                    await AnalogTakeCardToPostion(playerCardPile, playerHandManager, newCard);
                }
            }
            else if (card.ownerType == OwnerType.Enemy)
            {
                var newCard = enemyCardPile.GetRandomCard(CardData.CampType.Wu);
                if (newCard != null)
                {
                    await AnalogTakeCardToPostion(enemyCardPile, enemyHandManager, newCard);
                }
            }
        }
    }

    /// <summary>
    /// 平滑移动目标位置
    /// </summary>
    /// <param name="from">父控件</param>
    /// <param name="to">目标控件</param>
    /// <param name="card">卡牌</param>
    /// <returns></returns>
    private async Task FollowTargetPostion(Control from, Control to, Card card)
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

    /// <summary>
    /// 平滑移动目标位置
    /// </summary>
    /// <param name="from">父控件</param>
    /// <param name="to">目标控件</param>
    /// <param name="card">卡牌</param>
    /// <param name="transferPos">中转位置</param>
    /// <returns></returns>
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

    private async Task AnalogTakeCardToPostion(Control from, Control to, Card card)
    {
        card.ZIndex = 1;
        // 先把卡向上提高30像素
        var targetPos = card.GlobalPosition with { Y = card.GlobalPosition.Y - 30 };
        // 获取移动到的描点位置
        var targetPos2 = to.GlobalPosition;
        if (to is IDeck)
        {
            targetPos = (to as IDeck).GetMarkPosition();
        }
        // 模拟抽卡的动作，然后移动到指定父级
        var tween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "global_position", targetPos, takeDuration);
        tween.Chain().TweenProperty(card, "global_position", targetPos2, moveDuration);
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

    private string NumberToChineseOptimized(int num)
    {
        if (num < 1 || num > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(num));
        }
        var sb = new StringBuilder();
        // 数字映射
        string[] digits = ["", "一", "二", "三", "四", "五", "六", "七", "八", "九"];
        if (num < 10)
        {
            sb.Append(digits[num]);
        }
        else if (num < 20)
        {
            sb.Append('十');
            if (num > 10)
            {
                sb.Append(digits[num % 10]);
            }
        }
        else
        {
            sb.Append(digits[num / 10]);
            sb.Append('十');
            if (num % 10 != 0)
            {
                sb.Append(digits[num % 10]);
            }
        }
        return sb.ToString();
    }

    private async Task ShowTurnInfoAddBattlePanel(bool isShow)
    {
        if (isShow)
        {
            var tween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tween.SetParallel(true);
            tween.TweenProperty(battleLayer, "self_modulate:a", 1, 1.0f);
            tween.TweenProperty(turnLayer, "modulate:a", 1, 1.0f);
            await ToSignal(tween, Tween.SignalName.Finished);
        }
        else
        {
            battleLayer.SelfModulate = battleLayer.SelfModulate with { A = 0 };
            turnLayer.Modulate = turnLayer.Modulate with { A = 0 };
        }
    }

    private async Task CastSkillAsync(string skillScene, Vector2 startPos, Vector2 targerPos)
    {
        // 创建 TaskCompletionSource 来手动触发Task
        var tcs = new TaskCompletionSource();

        var skillNode = ResourceLoader.Load<PackedScene>(skillScene).Instantiate() as Node2D;
        skillLayer.AddChild(skillNode);
        skillNode.GlobalPosition = startPos;

        // 这里是不合适的做法，但是我这是卡牌回合制游戏，等待攻击动画完成不会影响游戏体验
        (skillNode as ISkillNode).ExecuteSkill(targerPos);
        (skillNode as ISkillNode).SkillPlayEnd += tcs.SetResult;

        // 等待委托执行完成
        await tcs.Task;
    }

    private int[] GetDistinctRandomNumbersPartialShuffle(int min, int max, int count)
    {
        if (count > (max - min))
            throw new ArgumentException("抽取数量不能超过范围大小");

        int total = max - min;
        int[] allNumbers = Enumerable.Range(min, total).ToArray();
        Random rnd = new Random();

        for (int i = 0; i < count; i++)
        {
            int j = rnd.Next(i, total);   // 在 [i, total-1] 中随机选一个交换
            (allNumbers[i], allNumbers[j]) = (allNumbers[j], allNumbers[i]);
        }

        int[] result = new int[count];
        Array.Copy(allNumbers, result, count);
        return result;
    }

    private void HandOutCards()
    {
        // 服务器抽卡，然后同步给客户端
        if (!IsMultiplayerAuthority()) return;

        if (cardTable.Count == 0) return;

        var tween = CreateTween();
        for (int i = 0; i < startCardNum; i++)
        {
            tween.TweenCallback(Callable.From(() =>
            {
                // 给玩家手牌
                Rpc(MethodName.GiveToCard, cardTable.RandomPopOne(), (int)OwnerType.Player);
            }));
            tween.TweenCallback(Callable.From(() =>
            {
                // 给敌人手牌
                Rpc(MethodName.GiveToCard, cardTable.RandomPopOne(), (int)OwnerType.Enemy);
            }));
            tween.TweenInterval(0.25f);
        }
        tween.Finished += NetxRound;
    }

    private void NetxRound()
    {
        // 客户端由服务器同步开始新回合
        if (!IsMultiplayerAuthority()) return;

        //GD.Print("主机手牌：" + playerHandManager.ToString());
        //GD.Print("客机手牌：" + enemyHandManager.ToString());

        currentRound++;

        NewRound();

        SynchronizeRoundByServer();
    }

    private void SynchronizeRoundByServer()
    {
        if (!IsMultiplayerAuthority()) return;

        var data = new Godot.Collections.Dictionary<string, Variant>
        {
            { "RoundTimerIsRunning", !roundTimer.IsStopped() },
            { "RoundTimerIsShow", timerLabel.Visible },
            { "RoundCount", currentRound },
        };
        Rpc(MethodName.SynchronizeRound, data);
    }

    private List<int> GetAllPeers()
    {
        List<int> allPeers = Multiplayer.GetPeers().ToList();
        // Multiplayer.GetPeers()不包含服务器的ID，本地多人中他是被需要的
        allPeers.Add((int)MultiplayerPeer.TargetPeerServer);
        return allPeers;
    }

    private void RequsetStartBattle()
    {
        // 由服务器处理
        if (!IsMultiplayerAuthority()) return;

        // 检查是否都已经出牌
        bool isAllPlayed = !peerPlayeds.Values.Any(x => x == false);
        if (isAllPlayed)
        {
            timerLabel.Visible = false;
            roundTimer.Stop();
            SynchronizeRoundByServer();

            Rpc(MethodName.StartBattle);
        }
    }

    private bool CheckAllPeersReady()
    {
        // 至少需要2个人才能开始游戏
        var allPeers = GetAllPeers();   
        if (allPeers.Count < 2) return false;

        foreach (var peerId in allPeers)
        {
            if (!readyPeerIds.Contains(peerId)) return false;
        }
        return true;
    }

    private void EndGame()
    {
        GetTree().Paused = false;

        // 停止网络连接
        Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

        // 重置服务器参数
        AppGlobalData.InitRoomData();

        GetTree().ChangeSceneToFile(lobbyScene);
    }

    #endregion
}
