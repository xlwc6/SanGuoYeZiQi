using Godot;
using System;

/// <summary>
/// 技能线程
/// </summary>
public partial class SkillProcess : Node
{
    /// <summary>
    /// 回调方法
    /// </summary>
    public Callable Callback;
    /// <summary>
    /// 执行时间
    /// </summary>
    public float Duration;

    private float _time; // 当前执行时间

    // GDSricpt的_init()类似C#的构造函数，在new时被调用
    public SkillProcess()
    {
        // 这里用GetTree()是获取不到节点的，因为在初始化时，还没添加到树中
        // 注意：构造函数中场景树未建立，这个可能为null
        (Engine.GetMainLoop() as SceneTree).CurrentScene.AddChild(this, true);
        //GetTree().CurrentScene.AddChild(this, true);
    }

    public override void _Process(double delta)
    {
        Callback.Call((float)delta);
        _time += (float)delta;
        if (_time >= Duration)
        {
            QueueFree();
            return;
        }
    }
}
