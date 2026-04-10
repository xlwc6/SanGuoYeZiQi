using Godot;
using Godot.Collections;
using System;
using System.Reflection.Emit;

/// <summary>
/// 冲击波
/// </summary>
public partial class ImpactBullet : Node2D, ISkillNode
{
    public event Action SkillPlayEnd;

    [Export]
    private AudioStream startAudio;
    [Export]
    private AudioStream goingAudio;
    [Export]
    private AudioStream endAudio;

    private SkillActuator skillActuator;
    private Laser2dWithLine laser;
    private AudioStreamPlayer audioPlayer;

    private Array<string> _skillQueue = []; // 技能组队列
    private Dictionary<string, Variant> skillData; // 技能数据

    private Vector2 targetPos; // 目标位置

    public override void _Ready()
    {
        skillActuator = GetNode<SkillActuator>("技能释放器");
        laser = GetNode<Laser2dWithLine>("激光射线");
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
        skillActuator.AddSkill("冲击波", new Dictionary<string, float> { { "冷却", 3.0f } });
        skillData = new Dictionary<string, Variant>
        {
            { "冲击波_发射", new Dictionary<string, float> { { "执行", 2.0f } } },
            { "冲击波_结束", new Dictionary<string, float> { { "执行", 0.6f } } },
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
            case "冲击波_发射":
                if (stage == "执行")
                {
                    PlayEffectAudio("冲击波_发射");
                    LookAt(targetPos);
                    
                    var total_distance = GlobalPosition.DistanceTo(targetPos); // 距离
                    var max_speed = (2 * total_distance) / duration; // 计算所需的最大速度
                    laser.CastSpeed = max_speed;

                    laser.IsCasting = true;
                }
                break;
            case "冲击波_结束":
                if (stage == "执行")
                {
                    laser.IsCasting = false;
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
            case "冲击波_发射":
                if (goingAudio != null)
                {
                    audioPlayer.Stream = goingAudio;
                    audioPlayer.Play();
                }
                break;
            case "冲击波_结束":
                if (endAudio != null)
                {
                    audioPlayer.Stream = endAudio;
                    audioPlayer.Play();
                }
                break;
            default: break;
        }
    }

    public void ExecuteSkill(Vector2 positon)
    {
        if (skillActuator.IsCanExecute("冲击波"))
        {
            // 添加追击目标
            targetPos = positon;

            // 添加技能组
            skillActuator.Execute("冲击波");
            _skillQueue.Add("冲击波_发射");
            _skillQueue.Add("冲击波_结束");

            ExecuteNextSkill();
        }
    }
}
