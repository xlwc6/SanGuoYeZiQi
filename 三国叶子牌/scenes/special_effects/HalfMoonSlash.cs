using Godot;
using Godot.Collections;
using System;
using System.Reflection;

/// <summary>
/// 半月斩
/// </summary>
public partial class HalfMoonSlash : Node2D, ISkillNode
{
    public event Action SkillPlayEnd;

    [Export]
    private AudioStream startAudio;
    [Export]
    private AudioStream goingAudio;
    [Export]
    private AudioStream endAudio;

    private SkillActuator skillActuator;
    private AnimatedSprite2D animatedSprite;
    private Area2D area;
    private AudioStreamPlayer audioPlayer;

    private Array<string> _skillQueue = []; // 技能组队列
    private Dictionary<string, Variant> skillData; // 技能数据

    private Vector2 targetPos; // 目标位置

    public override void _Ready()
    {
        skillActuator = GetNode<SkillActuator>("技能释放器");
        animatedSprite = GetNode<AnimatedSprite2D>("技能特效");
        area = GetNode<Area2D>("碰撞区域");
        audioPlayer = GetNode<AudioStreamPlayer>("技能音效");

        // 执行一个技能需要经过的几个阶段
        skillActuator.SetStages(["准备", "执行", "冷却"]);
        skillActuator.Ended += SkillActuator_Ended;

        InitSkillData();
    }

    public override void _ExitTree()
    {
        skillActuator.Ended -= SkillActuator_Ended;
    }

    private void SkillActuator_Ended(string skillName, string tag)
    {
        // 这里是等上面添加的技能组调用结束后，执行该技能下的小技能节点
        if (skillData.ContainsKey(skillName))
        {
            ExecuteNextSkill();
        }
        else
        {
            SkillPlayEnd?.Invoke();
            skillActuator.RemoveSkillAll();
            //否则销毁当前节点
            QueueFree();
        }
    }

    private void InitSkillData()
    {
        // 初始化技能数据
        skillActuator.AddSkill("半月斩", new Dictionary<string, float> { { "冷却", 4.0f } });
        skillData = new Dictionary<string, Variant>
        {
            { "半月斩_蓄力", new Dictionary<string, float> { { "执行", 0.6f } } },
            { "半月斩_释放", new Dictionary<string, float> { { "执行", 2.4f } } },
            { "半月斩_结束", new Dictionary<string, float> { { "执行", 0.5f } } },
        };
    }

    private void ExecuteNextSkill()
    {
        // 执行技能组下一个技能
        if (_skillQueue.Count == 0)
        {
            // 执行完了所有的小技能组，则此次技能执行完
            return;
        }
        // 执行这组技能中的小块技能
        var skillName = _skillQueue[0];
        _skillQueue.RemoveAt(0);
        if (!skillActuator.HasSkill(skillName))
        {
            // 获取技能每个阶段执行完所需时间数据
            var data = skillData[skillName].AsGodotDictionary<string, float>();
            skillActuator.AddSkill(skillName, data);
            var skill_node = skillActuator.GetSkill(skillName);
            // 这里不会自动断开连接，需手动调用RemoveSkill
            skill_node.ExecutedStage += SkillNodeExecutedStage;
        }
        // 执行
        skillActuator.Execute(skillName);
    }

    private void SkillNodeExecutedStage(object sender, string stage, float duration)
    {
        var skillNode = sender as TimeLine;
        // 根据技能名称和阶段，执行不同的方法
        switch (skillNode.SkillName)
        {
            case "半月斩_蓄力":
                if (stage == "执行")
                {
                    // 播放蓄力动画
                    animatedSprite.Play("半月斩_蓄力");
                    PlayEffectAudio("半月斩_蓄力");
                }
                break;
            case "半月斩_释放":
                if (stage == "执行")
                {
                    animatedSprite.Play("半月斩_追击");
                    PlayEffectAudio("半月斩_释放");
                    var start_position = GlobalPosition; // 起始位置
                    var move_timer = 0.0f; // 当前移动时间
                    var current_velocity = Vector2.Zero; // 当前速度
                    var total_distance = start_position.DistanceTo(targetPos); // 距离
                    var max_speed = (2 * total_distance) / duration; // 计算所需的最大速度
                    // 移动到目标位置，因为是连续的不是一次性完成的，用SkillProcess执行
                    var skill_process = new SkillProcess();
                    skill_process.Name = skillNode.SkillName;
                    skill_process.Duration = duration;
                    skill_process.Callback = Callable.From<float>((delta) =>
                    {
                        // 检查是否碰撞
                        if(area.HasOverlappingAreas())
                        {
                            skill_process.QueueFree();
                            skillNode.Goto("冷却");
                        }
                        move_timer += delta;
                        // 追击目标
                        var t = move_timer / duration;
                        if (t < 0.5)
                        {
                            var acceleration_factor = t * 2.0f;  // 0到1
                            current_velocity = (targetPos - start_position).Normalized() * max_speed * acceleration_factor;
                        }
                        else
                        {
                            current_velocity = (targetPos - start_position).Normalized() * max_speed;
                        }
                        GlobalPosition += current_velocity * delta;
                    });
                }
                break;
            case "半月斩_结束":
                if (stage == "执行")
                {
                    // 释放击中动画
                    animatedSprite.Play("半月斩_击中");
                    PlayEffectAudio("半月斩_结束");
                }
                break;
            default: break;
        }
    }

    private void PlayEffectAudio(string skillName)
    {
        if (audioPlayer.Playing)
        {
            audioPlayer.Stop();
        }
        switch (skillName)
        {
            case "半月斩_蓄力":
                if (startAudio != null)
                {
                    audioPlayer.Stream = startAudio;
                    audioPlayer.Play();
                }
                break;
            case "半月斩_释放":
                if (goingAudio != null)
                {
                    audioPlayer.Stream = goingAudio;
                    audioPlayer.Play();
                }
                break;
            case "半月斩_结束":
                if (endAudio != null)
                {
                    audioPlayer.Stream = endAudio;
                    audioPlayer.Play();
                }
                break;
            default: break;
        }
    }

    /// <summary>
    /// 执行技能
    /// </summary>
    public void ExecuteSkill(Vector2 positon)
    {
        if (skillActuator.IsCanExecute("半月斩"))
        {
            // 添加追击目标
            targetPos = positon;

            // 添加技能组
            skillActuator.Execute("半月斩");
            _skillQueue.Add("半月斩_蓄力");
            _skillQueue.Add("半月斩_释放");
            _skillQueue.Add("半月斩_结束");

            ExecuteNextSkill();
        }
    }
}
