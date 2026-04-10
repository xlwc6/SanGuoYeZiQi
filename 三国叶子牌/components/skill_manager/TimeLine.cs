using Godot;
using Godot.Collections;
using System;

/// <summary>
/// 执行技能功能时间线
/// </summary>
public partial class TimeLine : Node
{
    /* 
     * 资料：https://docs.godotengine.org/zh-cn/4.x/tutorials/scripting/c_sharp/c_sharp_signals.html
     * 这里使用自定义委托来处理，方便统一取消订阅，因为Godot中，对lamuda表达式的订阅不友好，不会自己断开
     */
    public delegate void ReadyExecuteEventHandler();
    public delegate void ExecutedStageEventHandler(object sender, string stage, float duration);
    public delegate void StoppedEventHandler();
    public delegate void PausedEventHandler();
    public delegate void ResumedEventHandler();
    public delegate void FinishedEventHandler();
    /// <summary>
    /// 准备执行
    /// </summary>
    public ReadyExecuteEventHandler ReadyExecute;
    /// <summary>
    /// 执行这个阶段时发出这个信号，包含当前时间阶段名称、和执行时间
    /// </summary>
    public ExecutedStageEventHandler ExecutedStage;
    /// <summary>
    /// 手动停止执行
    /// </summary>
    public StoppedEventHandler Stopped;
    /// <summary>
    /// 暂停执行
    /// </summary>
    public PausedEventHandler Paused;
    /// <summary>
    /// 继续执行
    /// </summary>
    public ResumedEventHandler Resumed;
    /// <summary>
    /// 执行完成
    /// </summary>
    public FinishedEventHandler Finished;

    /// <summary>
    /// 默认执行时间
    /// </summary>
    public const float DEFAULT_MIN_INTERVAL_TIME = 0;
    /// <summary>
    /// 执行方式
    /// </summary>
    public enum ProcessExecuteMode
    {
        Process, // _process 执行
        Physics, // _physics_process 执行
    }
    /// <summary>
    /// 执行状态
    /// </summary>
    public enum State
    {
        UnExecuted, // 未执行
        Executing, // 执行中
        Paused, // 暂停中
    }
    /// <summary>
    /// 所属技能名称
    /// </summary>
    public string SkillName;
    /// <summary>
    /// 时间阶段名称。这关系到 execute 方法中的数据获取的时间数据
    /// </summary>
    public Array<string> stages = [];
    /// <summary>
    /// process 执行方式
    /// </summary>
    public ProcessExecuteMode process_execute_mode = ProcessExecuteMode.Process;

    private float stage_time_left = 0.0f; // 修改这个时间会改变剩余时间
    /// <summary>
    /// 当前阶段的剩余时间
    /// </summary>
    public float GetTimeLeft => stage_time_left;

    private Dictionary<string, float> _last_data = [];
    /// <summary>
    /// 上次执行后的数据
    /// </summary>
    public Dictionary<string, float> LastData => _last_data;

    private int _stage_point = -1;
    /// <summary>
    /// 所在阶段的指针
    /// </summary>
    public int StagePoint
    {
        get { return _stage_point; }
        set
        {
            if (_stage_point != value)
            {
                _stage_point = value;
                // 修改时触发完成信号，一般都是再完成时修改指针
                if (_stage_point >= 0 && _stage_point < stages.Count)
                {
                    var stage_current = stages[_stage_point];
                    _last_data.TryGetValue(stage_current, out float duration_current);
                    ExecutedStage?.Invoke(this, stage_current, duration_current);
                }
            }
        }
    }


    private State _execute_state = State.UnExecuted;
    /// <summary>
    /// 当前执行到的阶段
    /// </summary>
    public State ExecuteState
    {
        get { return _execute_state; }
        set
        {
            if (_execute_state != value)
            {
                _execute_state = value;
                switch (_execute_state)
                {
                    case State.UnExecuted:
                    case State.Paused:
                        SetProcess(false);
                        SetPhysicsProcess(false);
                        break;
                    case State.Executing:
                        if (process_execute_mode == ProcessExecuteMode.Process)
                        {
                            SetProcess(true);
                        }
                        else if (process_execute_mode == ProcessExecuteMode.Physics)
                        {
                            SetPhysicsProcess(true);
                        }
                        break;
                    default: break;
                }
            }
        }
    }
    /// <summary>
    /// 是否正在执行
    /// </summary>
    /// <returns></returns>
    public bool IsExecuting() => _execute_state == State.Executing;

    public override void _Notification(int what)
    {
        if (what == NotificationReady)
        {
            SetProcess(false);
            SetPhysicsProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        Exec((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        Exec((float)delta);
    }

    /// <summary>
    /// 判断当前状态是否完成
    /// </summary>
    /// <param name="delta"></param>
    private void Exec(float delta)
    {
        stage_time_left -= delta;
        // 当前阶段执行完时，开始不断向后执行到 时间>0 的阶段
        while (stage_time_left <= 0)
        {
            StagePoint += 1;
            if (StagePoint < stages.Count)
            {
                stage_time_left += _last_data[stages[StagePoint]];
            }
            else
            {
                //所有阶段执行完毕
                stage_time_left = 0;
                StagePoint = -1;
                ExecuteState = State.UnExecuted;
                Finished?.Invoke();
                break;
            }
        }
    }

    /// <summary>
    /// 执行功能。执行名称 和 执行时间
    /// </summary>
    /// <param name="data"></param>
    public void Execute(Dictionary<string, float> data)
    {
        if (data == null | data.Count == 0)
        {
            GD.PushWarning("技能：时间线数据为空，没有执行功能");
            return;
        }
        // 拷贝一份，而非复制引用
        _last_data = data.Duplicate();
        StagePoint = 0;
        if (stages.Count > 0)
        {
            ExecuteState = State.Executing;
            foreach (var stage in stages)
            {
                if (data.TryGetValue(stage, out float num))
                {
                    _last_data[stage] = num;
                }
                else
                {
                    _last_data[stage] = DEFAULT_MIN_INTERVAL_TIME;
                }
            }

            // 执行时会先执行一下
            stage_time_left = _last_data[stages[0]];
            ReadyExecute?.Invoke();
            Exec(0);
        }
        else
        {
            GD.PushError("没有设置 stages，必须要设置每个执行的阶段的 key 值！");
        }
    }

    /// <summary>
    /// 停止执行
    /// </summary>
    public void Stop()
    {
        if (_execute_state == State.Executing)
        {
            ExecuteState = State.UnExecuted;
            Stopped?.Invoke();
        }
    }

    /// <summary>
    /// 暂停执行
    /// </summary>
    public void Pause()
    {
        if (_execute_state == State.Executing)
        {
            ExecuteState = State.Paused;
            Paused?.Invoke();
        }
    }

    /// <summary>
    /// 恢复执行
    /// </summary>
    public void Resume()
    {
        if (_execute_state == State.Paused)
        {
            ExecuteState = State.Executing;
            Resumed?.Invoke();
        }
    }

    /// <summary>
    /// 跳跃到这个阶段（不会触发 executed_stage 信号，需要手动发出）
    /// </summary>
    /// <param name="stage"></param>
    public void Goto(string stage)
    {
        if (_execute_state == State.Executing)
        {
            if (stages.Contains(stage))
            {
                StagePoint = stages.IndexOf(stage);
                stage_time_left = _last_data[stages[StagePoint]];
            }
            else
            {
                GD.PushWarning("stages 中没有 ", stage, "。 所有 stage: ", stages);
            }
        }
    }

    /// <summary>
    /// 断开全部事件订阅
    /// </summary>
    public void DisconnectEvent()
    {
        // 这里本来想写在析构函数里，但是不清楚Godot会不会有特殊处理，还是单独写个调用
        ReadyExecute = null;
        ExecutedStage = null;
        Stopped = null;
        Paused = null;
        Resumed = null;
        Finished = null;
    }
}
