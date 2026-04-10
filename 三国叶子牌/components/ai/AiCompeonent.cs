using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI组件
/// </summary>
public partial class AiCompeonent : Node
{
    /// <summary>
    /// AI是否启用
    /// </summary>
    [Export]
    public bool AiEnabled = true;

    private HandManager enemyHandManager;
    private CardPile enemyCardPile;
    private CardPile playerCardPile;

    private Dictionary<string, string> allCardRes = [];// 全部的卡牌
    private List<string> usedCard = []; // 已知的卡牌集合
    private List<CardData> handCards = [];// 自己的手牌
    private List<CardData> playedCards = [];// 已经使用过的牌
    private List<CardData> playerPossibleCards = [];// 玩家可能的手牌
    private Dictionary<string, float> handStrength = [];// 手牌强度集合
    private WeightedTable<string> dmTable = new("决策池");

    public override void _Ready()
    {
        /*
         * 敌人AI分析
         * 1、检索自身的手牌
         * 2、拥有记牌器，可以记录自己和玩家已经使用过的牌
         * 3、每次出牌随机选择一个决策类型，每次加载时决策权重值随机
         * 4、决策类分为3种：全随机、赌徒、模拟
         * 5、全随机-靠概率运气，赌徒-出手牌强度最大的，模拟-与未知的牌进行模拟对战，出胜率高的
         */
        enemyHandManager = GetTree().GetFirstNodeInGroup("enemy_deck") as HandManager;
        enemyCardPile = GetTree().GetFirstNodeInGroup("enemy_discard_deck") as CardPile;
        playerCardPile = GetTree().GetFirstNodeInGroup("player_discard_deck") as CardPile;

        InitDMTable();
        InitCardRes();
    }

    // 初始化决策权重
    private void InitDMTable()
    {
        dmTable.Clear();
        foreach (var value in Enum.GetValues(typeof(DecisionMakingType)))
        {
            int rd = GD.RandRange(1, 10);
            dmTable.Add(value.ToString(), rd);
        }
    }

    // 初始化全部卡牌资源数据
    private void InitCardRes()
    {
        allCardRes.Clear();
        // 所以的卡牌数据应该以JSON或者其他格式存储地址，这里由于游戏体量小就无所谓
        allCardRes.Add("刘协", "res://resources/card_data/qun/刘协.tres");
        allCardRes.Add("吕布", "res://resources/card_data/qun/吕布.tres");
        allCardRes.Add("张角", "res://resources/card_data/qun/张角.tres");
        allCardRes.Add("文丑", "res://resources/card_data/qun/文丑.tres");
        allCardRes.Add("袁绍", "res://resources/card_data/qun/袁绍.tres");
        allCardRes.Add("貂蝉", "res://resources/card_data/qun/貂蝉.tres");
        allCardRes.Add("颜良", "res://resources/card_data/qun/颜良.tres");
        allCardRes.Add("关羽", "res://resources/card_data/shu/关羽.tres");
        allCardRes.Add("刘备", "res://resources/card_data/shu/刘备.tres");
        allCardRes.Add("姜维", "res://resources/card_data/shu/姜维.tres");
        allCardRes.Add("张飞", "res://resources/card_data/shu/张飞.tres");
        allCardRes.Add("法正", "res://resources/card_data/shu/法正.tres");
        allCardRes.Add("诸葛亮", "res://resources/card_data/shu/诸葛亮.tres");
        allCardRes.Add("赵云", "res://resources/card_data/shu/赵云.tres");
        allCardRes.Add("马超", "res://resources/card_data/shu/马超.tres");
        allCardRes.Add("黄忠", "res://resources/card_data/shu/黄忠.tres");
        allCardRes.Add("典韦", "res://resources/card_data/wei/典韦.tres");
        allCardRes.Add("司马懿", "res://resources/card_data/wei/司马懿.tres");
        allCardRes.Add("夏侯渊", "res://resources/card_data/wei/夏侯渊.tres");
        allCardRes.Add("张辽", "res://resources/card_data/wei/张辽.tres");
        allCardRes.Add("曹操", "res://resources/card_data/wei/曹操.tres");
        allCardRes.Add("许褚", "res://resources/card_data/wei/许褚.tres");
        allCardRes.Add("郭嘉", "res://resources/card_data/wei/郭嘉.tres");
        allCardRes.Add("周泰", "res://resources/card_data/wu/周泰.tres");
        allCardRes.Add("周瑜", "res://resources/card_data/wu/周瑜.tres");
        allCardRes.Add("太史慈", "res://resources/card_data/wu/太史慈.tres");
        allCardRes.Add("孙权", "res://resources/card_data/wu/孙权.tres");
        allCardRes.Add("甘宁", "res://resources/card_data/wu/甘宁.tres");
        allCardRes.Add("陆逊", "res://resources/card_data/wu/陆逊.tres");
        allCardRes.Add("鲁肃", "res://resources/card_data/wu/鲁肃.tres");
    }

    // 初始化AI掌握的卡牌数据
    private void InitCardData()
    {
        usedCard.Clear();
        handCards.Clear();
        playedCards.Clear();
        playerPossibleCards.Clear();
        // 重新计算当前卡牌数据
        foreach (var item in enemyHandManager.GetCardDatas())
        {
            handCards.Add(item);
            usedCard.Add(item.Name);
        }
        foreach (var item in enemyCardPile.GetCardDatas())
        {
            playedCards.Add(item);
            usedCard.Add(item.Name);
        }
        foreach (var item in playerCardPile.GetCardDatas())
        {
            playedCards.Add(item);
            usedCard.Add(item.Name);
        }
        foreach (var item in allCardRes.Keys)
        {
            if (usedCard.Contains(item))
            {
                continue;
            }
            var cardData = ResourceLoader.Load<CardData>(allCardRes[item]);
            playerPossibleCards.Add(cardData);
        }
        // 计算卡牌强度
        EvaluateHandStrength();
    }

    // 手牌强度评估
    private void EvaluateHandStrength()
    {
        handStrength.Clear();
        /*
         * 分值公式：强度 = 武力 + 智力 * (技能 / 100 + K)
         * 技能类型分值：战前 90、战中 70、战后 50
         * K：3 / 7 * 0.1 为武力和智力的分值比重，
         */
        float k = 3 / 7 * 0.1f;
        foreach (var item in handCards)
        {
            float mid = item.Type == CardData.SkillType.Before ? 90 : (item.Type == CardData.SkillType.After ? 50 : 70);
            var score = item.Power + item.Wisdom * (mid / 100 + k);
            handStrength.Add(item.Name, score);
            //GD.Print($"{item.Name}，得分:{score}");
        }
    }

    // 根据全随机类型获取一张手牌
    private string GetCardByFullRandomized()
    {
        int index = GD.RandRange(0, handCards.Count - 1);
        var card = handCards[index];
        return card.Name;
    }

    // 根据赌徒类型获取一张手牌
    private string GetCardByGambler()
    {
        var maxItme = handStrength.MaxBy(x => x.Value);
        return maxItme.Key;
    }

    // 根据模拟类型获取一张手牌
    private string GetCardBySimulate()
    {
        Dictionary<string, int> numberOfWins = [];
        // 遍历比较
        foreach (var card in playerPossibleCards)
        {
            foreach (var hand in handCards)
            {
                // 只统计胜利的
                if (hand.Compare(card) <= 0) continue;
                if (!numberOfWins.TryAdd(hand.Name, 1))
                {
                    numberOfWins[hand.Name] += 1;
                }
            }
        }
        // 如果一张都赢不了，那就用分值最高的
        if(numberOfWins.Count == 0)
        {
            return GetCardByGambler();
        }
        // 获取胜利次数最大
        int? maxValue = null;
        List<string> maxItems = [];
        foreach (var kvp in numberOfWins)
        {
            if (maxValue == null || kvp.Value > maxValue)
            {
                // 发现新的最大值，清空列表并更新最大值
                maxValue = kvp.Value;
                maxItems.Clear();
                maxItems.Add(kvp.Key);
            }
            else if (kvp.Value == maxValue)
            {
                // 与当前最大值相等，添加到列表
                maxItems.Add(kvp.Key);
            }
        }
        // 如果集合个数不为1，就当中获取强度最大的
        if (maxItems.Count == 1) return maxItems[0];
        string result = maxItems[0];
        for (int i = 1; i < maxItems.Count - 1; i++)
        {
            string temp = maxItems[i];
            if (handStrength[result] < handStrength[temp])
            {
                result = temp;
            }
        }
        return result;
    }

    // 多维性格系统：将三个维度离散化为高中低，然后查表映射
    private PersonalityType CalculateMultidimensional(float careless, float moderate, float cautious)
    {
        // 设定阈值 low: 占比 < 0.3，mid: 0.3 ≤ 占比 ≤ 0.6，hight: 占比 > 0.6
        // 离散化函数：0=低, 1=中, 2=高
        static int Discretize(float val)
        {
            if (val < 0.3f) return 0;      // 低
            else if (val < 0.6f) return 1; // 中
            else return 2;                  // 高
        }

        int codeC = Discretize(careless);
        int codeM = Discretize(moderate);
        int codeCa = Discretize(cautious);

        // 定义映射表（三元组 -> 性格）
        var map = new Dictionary<(int, int, int), PersonalityType>
        {
            { (1, 0, 0), PersonalityType.Careless },        // 粗心中
            { (0, 1, 0), PersonalityType.Moderate },        // 中庸中
            { (0, 0, 1), PersonalityType.Cautious },        // 谨慎中
            { (2, 0, 0), PersonalityType.Adventurer },      // 粗心高
            { (0, 2, 0), PersonalityType.Easygoing },       // 中庸高
            { (0, 0, 2), PersonalityType.Strategist },      // 谨慎高
            { (2, 1, 0), PersonalityType.Impulsive },       // 粗心高 + 中庸中
            { (1, 0, 1), PersonalityType.Contradictory },   // 粗心中 + 谨慎中
            { (2, 0, 1), PersonalityType.Contradictory },   // 粗心高 + 谨慎中
            { (1, 0, 2), PersonalityType.Contradictory },   // 粗心中 + 谨慎高
            { (1, 2, 0), PersonalityType.Impulsive },       // 粗心中 + 中庸高
            { (0, 2, 1), PersonalityType.Steady },          // 中庸高 + 谨慎中
            { (0, 1, 1), PersonalityType.Steady },          // 中庸中 + 谨慎中
            { (0, 1, 2), PersonalityType.Steady },          // 中庸中 + 谨慎高
            { (1, 1, 1), PersonalityType.Balanced },        // 三者均衡
        };

        var key = (codeC, codeM, codeCa);
        if (map.TryGetValue(key, out PersonalityType result))
            return result;

        return PersonalityType.Undefined;
    }

    /// <summary>
    /// 获取性格特征描述
    /// </summary>
    public string GetPersonality()
    {
        float c_pcte = 0, z_pcte = 0, j_pctz = 0;
        // 权重高到低排序
        var sortedItems = dmTable.GetSortedByWeightDescending().ToList();
        // 计算占比
        foreach (var item in sortedItems)
        {
            var probability = item.Value / dmTable.TotalWeight;
            if (item.Key == "FullRandomized")
            {
                c_pcte = (float)probability;
            }
            if (item.Key == "Gambler")
            {
                z_pcte = (float)probability;
            }
            if (item.Key == "Simulate")
            {
                j_pctz = (float)probability;
            }
        }
        // 获取性格类型
        var p_type = CalculateMultidimensional(c_pcte, z_pcte, j_pctz);
        return $"性格为：{p_type.GetDescription()}，三维占比为：粗心值：({c_pcte:F2}%)、中庸值：({z_pcte:F2}%)、谨慎值：({j_pctz:F2}%)";
    }

    /// <summary>
    /// 出牌
    /// </summary>
    /// <returns></returns>
    public void PlayCard()
    {
        if (!AiEnabled) return;

        // 初始化手牌
        InitCardData();

        string cardName = string.Empty;
        // 抽取决策权重
        DecisionMakingType dm = (DecisionMakingType)Enum.Parse(typeof(DecisionMakingType), dmTable.RandomSelect());
        cardName = dm switch
        {
            DecisionMakingType.FullRandomized => GetCardByFullRandomized(),
            DecisionMakingType.Gambler => GetCardByGambler(),
            DecisionMakingType.Simulate => GetCardBySimulate(),
            _ => GetCardByFullRandomized(),
        };
        // 把抽到的牌从手牌打出
        var playingCard = enemyHandManager.GetCard(cardName);
        playingCard.ChangedToState(BaseState.State.Played);
    }
}
