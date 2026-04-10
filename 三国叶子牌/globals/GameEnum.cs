using System;
using System.ComponentModel;

/// <summary>
/// 拥有者
/// </summary>
public enum OwnerType
{
    None,
    Player,
    Enemy,
}

/// <summary>
/// 卡堆类型
/// </summary>
public enum CardPileType
{
    Tiled, // 平铺
    Stack, // 堆叠
}

/// <summary>
/// 音频总线
/// </summary>
public enum BusType
{
    Master,
    SFX,
    BGM
}

/// <summary>
/// 决策类型
/// </summary>
public enum DecisionMakingType
{
    FullRandomized, // 全随机
    Gambler, // 赌徒
    Simulate, // 模拟
}

/// <summary>
/// 性格类型
/// </summary>
public enum PersonalityType
{
    [Description("混合型")]
    Undefined,
    [Description("粗心型")]
    Careless,
    [Description("中庸型")]
    Moderate,
    [Description("谨慎型")]
    Cautious,
    [Description("平衡型")]
    Balanced,
    [Description("冒险家")]
    Adventurer,
    [Description("随和者")]
    Easygoing,
    [Description("谋略家")]
    Strategist,
    [Description("冲动派")]
    Impulsive,
    [Description("矛盾体")]
    Contradictory,
    [Description("稳健者")]
    Steady,
}
