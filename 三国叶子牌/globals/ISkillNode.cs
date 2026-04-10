using Godot;
using System;

/// <summary>
/// 技能类
/// </summary>
public interface ISkillNode
{
    /// <summary>
    /// 技能播放完后的回调
    /// </summary>
    public event Action SkillPlayEnd;

    /// <summary>
    /// 执行技能
    /// </summary>
    public void ExecuteSkill(Vector2 targetPos);
}
