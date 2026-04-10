using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 技能执行者
/// 参考: https://github.com/LaoDie1/godot-skill-demo
/// </summary>
[GlobalClass]
public partial class SkillActuator : Node
{
    /// <summary>
    /// 添加了新技能
    /// </summary>
    public event Action<string> SkillAdded;
    /// <summary>
    /// 移除了新技能
    /// </summary>
    public event Action<string> SkillRemoved;
    /// <summary>
    /// 技能开始执行
    /// </summary>
    public event Action<string> Started;
    /// <summary>
    /// 打断技能
    /// </summary>
    public event Action<string> Interruptted;
    /// <summary>
    /// 已强行停止技能
    /// </summary>
    public event Action<string> Stopped;
    /// <summary>
    /// 执行完成
    /// </summary>
    public event Action<string> Finished;
    /// <summary>
    /// 执行中止、打断、停止、执行完成都会调用这个信号，包含技能名称，被打断的事件名
    /// </summary>
    public event Action<string, string> Ended;

    /// <summary>
    /// 技能状态
    /// </summary>
    public enum State
    {
        UnExecuted = -1, //未执行
        NotExistent = -2, // 技能不存在
    }

    /// <summary>
    /// 技能执行阶段。调用 add_skill 时，传入的 Dictionary 数据中的 key 如果有这个阶段的值;
    /// 则获取这个数据的值为播放时间数据，否则播放时间按照默认 TimeLine.DEFAULT_MIN_INTERVAL_TIME
    /// </summary>
    [Export]
    protected Godot.Collections.Array<string> stages = [];
    /// <summary>
    /// 忽略缺省的数据中的 key。如果这个属性为 true，
    /// </summary>
    [Export]
    protected bool IgnoreDefaultKey = true;

    /// <summary>
    /// 当前正在执行的技能
    /// </summary>
    private Godot.Collections.Dictionary<string, TimeLine> _current_execute_skills = [];
    /// <summary>
    /// 技能名称对应的技能节点
    /// </summary>
    private Godot.Collections.Dictionary<string, TimeLine> _name_to_skill_map = [];
    /// <summary>
    /// 技能名称对应的技能数据
    /// </summary>
    private Godot.Collections.Dictionary<string, Godot.Collections.Dictionary<string, float>> _name_to_data_map = [];

    #region 基础方法

    /// <summary>
    /// 获取技能
    /// </summary>
    public TimeLine GetSkill(string skillName)
    {
        if (_name_to_skill_map.TryGetValue(skillName, out TimeLine skill))
        {
            return skill;
        }
        else
        {
            GD.PrintErr("没有这个技能：", skillName);
            return null;
        }
    }

    /// <summary>
    /// 获取技能名称列表
    /// </summary>
    /// <returns></returns>
    public List<string> GetSkillNameList()
    {
        return _name_to_skill_map.Keys.ToList();
    }

    /// <summary>
    /// 设置技能执行几个阶段的值（按顺序），如果不设置则在 add_skill 的时候添加的数据的时候没有执行时间
    /// </summary>
    public void SetStages(string[] arr)
    {
        stages.Clear();
        foreach (var item in arr)
        {
            if (!stages.Contains(item))
            {
                stages.Add(item);
            }
        }
    }

    /// <summary>
    /// 获取这个 stage 索引的阶段的名称
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public string GetStageName(int index)
    {
        if (index >= 0 && index < stages.Count)
        {
            return stages[index];
        }
        return null;
    }

    /// <summary>
    /// 获取正在执行的技能名称列表
    /// </summary>
    /// <returns></returns>
    public List<string> GetExecutingSkills()
    {
        return _current_execute_skills.Keys.ToList();
    }

    /// <summary>
    /// 技能是否正在执行
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public bool IsExecuting(string skillName)
    {
        return _current_execute_skills.ContainsKey(skillName);
    }

    /// <summary>
    /// 是否有这个技能
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public bool HasSkill(string skillName)
    {
        return _name_to_skill_map.ContainsKey(skillName);
    }

    /// <summary>
    /// 技能能否执行
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public bool IsCanExecute(string skillName)
    {
        return HasSkill(skillName) && !IsExecuting(skillName);
    }

    /// <summary>
    /// 添加技能
    /// 技能中需要有 stages 中的 key，和 执行时间；
    /// 例如：["ready", "before", "execute", "after"]，
    /// 则：{ "ready": 0.1,  "before": 0, "execute": 1.0, "after": 0 }
    /// </summary>
    public void AddSkill(string skillName, Godot.Collections.Dictionary<string, float> data)
    {
        if (!IgnoreDefaultKey)
        {
            foreach (var item in data.Keys)
            {
                if (!stages.Contains(item))
                {
                    GD.PrintErr("stages 中不存在", item);
                    return;
                }
            }
        }

        // 创建技能
        if (_name_to_data_map.ContainsKey(skillName))
        {
            return;
        }
        _name_to_data_map.Add(skillName, data);
        var skill = new TimeLine
        {
            process_execute_mode = TimeLine.ProcessExecuteMode.Physics,
            stages = stages,
            SkillName = skillName
        };
        // 添加节点到技能管理器
        AddChild(skill);
        _name_to_skill_map.Add(skillName, skill);

        // 执行时的
        skill.ReadyExecute += () =>
        {
            _current_execute_skills[skillName] = null;
            Started?.Invoke(skillName);
        };
        skill.Resumed += () =>
        {
            _current_execute_skills[skillName] = null;
        };

        // 执行结束
        skill.Finished += () =>
        {
            _current_execute_skills.Remove(skillName);
            Finished?.Invoke(skillName);
            Ended?.Invoke(skillName, "Finished");
        };
        skill.Paused += () =>
        {
            _current_execute_skills.Remove(skillName);
            Interruptted?.Invoke(skillName);
            Ended?.Invoke(skillName, "Interruptted");
        };
        skill.Stopped += () =>
        {
            _current_execute_skills.Remove(skillName);
            Stopped?.Invoke(skillName);
            Ended?.Invoke(skillName, "Stopped");
        };

        // 新增技能
        SkillAdded?.Invoke(skillName);
    }

    /// <summary>
    /// 移除技能
    /// </summary>
    /// <param name="skillName"></param>
    public void RemoveSkill(string skillName)
    {
        if (_name_to_skill_map.TryGetValue(skillName, out TimeLine skill))
        {
            _name_to_skill_map.Remove(skillName);
            SkillRemoved?.Invoke(skillName);
            // 移除订阅事件
            skill.DisconnectEvent();
            // 移除节点
            RemoveChild(skill);
        }
    }

    public void RemoveSkillAll()
    {
        foreach (var skillName in _name_to_skill_map.Keys)
        {
            _name_to_skill_map.Remove(skillName);
            SkillRemoved?.Invoke(skillName);
            // 移除订阅事件
            if (_name_to_skill_map.TryGetValue(skillName, out TimeLine skill))
            {
                skill.DisconnectEvent();
                // 移除节点
                RemoveChild(skill);
            }
        }
    }

    /// <summary>
    /// 获取技能执行到的阶段。如果没有在执行则返回 -1，如果没有这个技能则返回 -2
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public int GetSkillStage(string skillName)
    {
        var skill = GetSkill(skillName);
        if (skill != null)
        {
            return (int)skill.ExecuteState;
        }
        return (int)State.NotExistent;
    }

    /// <summary>
    /// 获取这个技能的数据。可以通过修改这个数据改变执行的技能的数据
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public Godot.Collections.Dictionary<string, float> GetSkillData(string skillName)
    {
        if (HasSkill(skillName))
        {
            return _name_to_data_map[skillName];
        }
        return default;
    }

    /// <summary>
    /// 获取这个技能执行状态的名称
    /// </summary>
    /// <param name="skillName"></param>
    /// <returns></returns>
    public string GetSkillStageName(string skillName)
    {
        if (HasSkill(skillName))
        {
            var stage = GetSkillStage(skillName);
            return GetStageName(stage);
        }
        return null;
    }

    /// <summary>
    /// 这个技能当前是否正在这个阶段中运行
    /// </summary>
    /// <param name="skillName"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool IsInStage(string skillName, int index)
    {
        var skill = GetSkill(skillName);
        if (skill != null)
        {
            return skill.StagePoint == index;
        }
        return false;
    }

    #endregion

    #region 主要接口

    /// <summary>
    /// 执行技能
    /// </summary>
    /// <param name="skillName"></param>
    /// <param name="data"></param>
    public void Execute(string skillName, Godot.Collections.Dictionary<string, float> additional = null)
    {
        if (stages.Count == 0)
        {
            GD.PushWarning("没有设置执行阶段的值！");
            return;
        }
        var skill = GetSkill(skillName);
        if (skill != null)
        {
            // 合并技能参数
            var data = GetSkillData(skillName);
            if (additional != null)
            {
                data.Merge(additional, true);
            }
            if (data == null || data.Count == 0)
            {
                GD.PushWarning("没有执行的功能的数据");
                return;
            }
            skill.Execute(data);
        }
    }

    /// <summary>
    /// 继续执行技能
    /// </summary>
    /// <param name="skillName"></param>
    public void ContinueExecute(string skillName)
    {
        var skill = GetSkill(skillName);
        if (skill != null && IsExecuting(skillName))
        {
            skill.Resume();
        }
    }

    /// <summary>
    /// 打断技能，中止技能的执行，可以继续执行
    /// </summary>
    /// <param name="skillName"></param>
    public void Interrupt(string skillName)
    {
        var skill = GetSkill(skillName);
        if (skill != null)
        {
            skill.Pause();
        }
    }

    /// <summary>
    /// 停止技能
    /// </summary>
    /// <param name="skillName"></param>
    public void Stop(string skillName)
    {
        var skill = GetSkill(skillName);
        if (skill != null)
        {
            skill.Stop();
        }
    }

    /// <summary>
    /// 跳到某个阶段执行
    /// </summary>
    public void GotoStage(string skillName)
    {
        var skill = GetSkill(skillName);
        if (skill != null && skill.IsExecuting())
        {
            skill.Stop();
        }
    }

    #endregion
}
