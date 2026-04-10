using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 状态机
/// </summary>
[GlobalClass]
public partial class StateMachine : Node
{
    [Export]
    private BaseState startingState; // 初始状态

    private List<BaseState> stateList = new List<BaseState>();

    private BaseState currentState; // 当前状态

    public override void _Ready()
    {
        foreach (BaseState state in GetChildren().Cast<BaseState>())
        {
            state.Initialize(this, GetParent<Card>());
            stateList.Add(state);
        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <returns></returns>
    public BaseState.State GetCurrentState()
    {
        return currentState.state;
    }

    /// <summary>
    /// 启动状态机
    /// </summary>
    public void LaunchStateMachine()
    {
        currentState = startingState;
        currentState.OnStateEnter();
    }

    /// <summary>
    /// 当节点收到 InputEvent 时发出
    /// </summary>
    /// <param name="e"></param>
    public void OnGuiInput(InputEvent e)
    {
        currentState.OnGuiInput(e);
    }

    /// <summary>
    /// 有输入事件时会被调用。输入事件会沿节点树向上传播，直到有节点将其消耗。
    /// </summary>
    /// <param name="e"></param>
    public void OnInput(InputEvent e)
    {
        currentState.OnInput(e);
    }

    /// <summary>
    /// 鼠标移入事件
    /// </summary>
    public void OnMouseEnter()
    {
        currentState.OnMouseEnter();
    }

    /// <summary>
    /// 鼠标移出事件
    /// </summary>
    public void OnMouseExit()
    {
        currentState.OnMouseExit();
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void ChangeToState<T>() where T : BaseState
    {
        BaseState newState = stateList.FirstOrDefault(x => x is T);
        if (newState == null) return;
        currentState.OnStateExit();
        currentState = newState;
        currentState.OnStateEnter();
    }
}
