using Godot;
using System;

/// <summary>
/// 基础状态
/// </summary>
[GlobalClass]
public partial class BaseState : Node
{
    public enum State
    {
        Normal,  // 正常
        Clicked, // 点击
        Played,  // 使用
        Discard, // 丢弃
    }

    /// <summary>
    /// 所属状态
    /// </summary>
    [Export]
    public State state;

    /// <summary>
    /// 状态机
    /// </summary>
    protected StateMachine stateMachine;
    /// <summary>
    /// 状态机拥有者
    /// </summary>
    protected Card card;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="machine">状态机</param>
    /// <param name="owner">状态机拥有者</param>
    public virtual void Initialize(StateMachine machine, Card owner)
    {
        stateMachine = machine;
        card = owner;
    }

    /// <summary>
    /// 进入该状态时被调用一次
    /// </summary>
    public virtual void OnStateEnter()
    {

    }

    /// <summary>
    /// 退出该状态时被调用一次
    /// </summary>
    /// <returns></returns>
    public virtual void OnStateExit()
    {

    }

    /// <summary>
    /// 当节点收到 InputEvent 时发出
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnGuiInput(InputEvent e) 
    {

    }

    /// <summary>
    /// 有输入事件时会被调用。输入事件会沿节点树向上传播，直到有节点将其消耗。
    /// </summary>
    /// <param name="e"></param>
    public virtual void OnInput(InputEvent e)
    {

    }

    /// <summary>
    /// 鼠标移入事件
    /// </summary>
    public virtual void OnMouseEnter()
    {

    }

    /// <summary>
    /// 鼠标移出事件
    /// </summary>
    public virtual void OnMouseExit()
    {

    }
}
