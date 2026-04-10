using Godot;
using System;

/// <summary>
/// 游戏全局事件
/// 如果你在_ready()函数中连接信号，而该节点被实例化多次，那么每个实例都会连接一次
/// </summary>
public partial class GameEvent : Node
{
    /// <summary>
    /// 卡牌被点击
    /// </summary>
    [Signal]
    public delegate void CardClickedEventHandler(Card withCard);

    /// <summary>
    /// 卡牌被释放
    /// </summary>
    [Signal]
    public delegate void CardReleasedEventHandler(Card withCard);

    /// <summary>
    /// 卡牌被使用
    /// </summary>
    [Signal]
    public delegate void CardPlayedEventHandler(Card withCard);

    /// <summary>
    /// 受到伤害
    /// </summary>
    [Signal]
    public delegate void BeHurtEventHandler(int owner, int damage);

    /// <summary>
    /// 角色死亡
    /// </summary>
    [Signal]
    public delegate void DeathEventHandler(int owner);

    /// <summary>
    /// 回合开始
    /// </summary>
    [Signal]
    public delegate void TurnBeginEventHandler();

    /// <summary>
    /// 回合结束
    /// </summary>
    [Signal]
    public delegate void TurnEndEventHandler();

    /// <summary>
    /// 跳过下一回合
    /// </summary>
    [Signal]
    public delegate void TurnSkipNextEventHandler();

    /// <summary>
    /// 丢弃卡牌
    /// </summary>
    [Signal]
    public delegate void CardDiscardedEventHandler(int owner, string[] cards);

    /// <summary>
    /// 请求继续游戏
    /// </summary>
    [Signal]
    public delegate void ContinueGameEventHandler(int peerId);

    /// <summary>
    /// 退出本局游戏
    /// </summary>
    [Signal]
    public delegate void QuitGameRequestedEventHandler();
}
