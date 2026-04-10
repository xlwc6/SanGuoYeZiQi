using Godot;
using System;

public partial class Laser2dWithLine : RayCast2D
{
    /*
     * 教程：https://www.gdquest.com/library/laser_2d/
     */
    [Export]
    private Color line_color = Colors.White; // 线条颜色
    public Color LineColor
    {
        get { return line_color; }
        set
        {
            SetColor(value);
        }
    }
    [Export]
    private bool is_casting = false; // 是否发射
    public bool IsCasting
    {
        get { return is_casting; }
        set
        {
            if (is_casting == value) return;
            SetIsCasting(value);
        }
    }
    [Export]
    private float cast_speed = 7000.0f; // 射线速度
    public float CastSpeed
    {
        get { return cast_speed; }
        set { cast_speed = value; }
    }
    [Export]
    private float max_length = 1000.0f; // 最大长度
    public float MaxLength
    {
        get { return max_length; }
        set { max_length = value; }
    }
    [Export]
    private float growth_time = 0.1f; // 生长时间

    private Line2D line_2d;
    private GpuParticles2D casting_particles;
    private GpuParticles2D collision_particles;
    private GpuParticles2D beam_particles;

    private Tween tween;

    public override void _Ready()
    {
        line_2d = GetNode<Line2D>("纹理线条");
        casting_particles = GetNode<GpuParticles2D>("射线颗粒");
        collision_particles = GetNode<GpuParticles2D>("碰撞颗粒");
        beam_particles = GetNode<GpuParticles2D>("光束颗粒");

        SetColor(line_color);
        SetIsCasting(is_casting);

        /*
         * 在 Godot C# 中，Line2D.Points 的 get 访问器返回的是内部 PackedVector2Array 的托管数组副本
         * 只有通过 set 将新数组传回去，引擎才会更新线条几何体
         */
        var c_points = line_2d.Points;
        c_points[0] = Vector2.Right;
        c_points[1] = Vector2.Zero;
        line_2d.Points = c_points;

        line_2d.Visible = false;
        casting_particles.Position = line_2d.Points[0];
    }

    public override void _PhysicsProcess(double delta)
    {
        TargetPosition = TargetPosition.MoveToward(Vector2.Right * max_length, cast_speed * (float)delta);
        var laser_end_position = TargetPosition;
        // 获取最新的碰撞，避免当raycast的enabled属性为true时出现的一帧延迟
        ForceRaycastUpdate();

        // 如果有碰撞，更新线条结束位置
        if (IsColliding())
        {
            laser_end_position = ToLocal(GetCollisionPoint());
            collision_particles.GlobalRotation = GetCollisionNormal().Angle();
            collision_particles.Position = laser_end_position;
        }

        var c_points = line_2d.Points;
        c_points[1] = laser_end_position;
        line_2d.Points = c_points;

        var laser_start_position = line_2d.Points[0];
        beam_particles.Position = laser_start_position + (laser_end_position - laser_start_position) * 0.5f;
        var beam_particles_material = beam_particles.ProcessMaterial as ParticleProcessMaterial;
        var new_material_x = laser_end_position.DistanceTo(laser_start_position) * 0.5f;
        beam_particles_material.EmissionBoxExtents = beam_particles_material.EmissionBoxExtents with { X = new_material_x };

        collision_particles.Emitting = IsColliding();
    }

    /// <summary>
    /// 设置射线颜色
    /// </summary>
    /// <param name="newColor"></param>
    private void SetColor(Color newColor)
    {
        line_color = newColor;
        if (line_2d == null) return;
        line_2d.Modulate = newColor;
        casting_particles.Modulate = newColor;
        collision_particles.Modulate = newColor;
        beam_particles.Modulate = newColor;
    }

    /// <summary>
    /// 设置是否发射
    /// </summary>
    /// <param name="newValue"></param>
    private void SetIsCasting(bool newValue)
    {
        is_casting = newValue;
        
        SetPhysicsProcess(is_casting);
        if (beam_particles == null) return;

        beam_particles.Emitting = is_casting;
        casting_particles.Emitting = is_casting;

        if (!is_casting)
        {
            TargetPosition = Vector2.Zero;
            collision_particles.Emitting = false;

            Disappear();
        }
        else
        {
            var laser_start = Vector2.Right;

            var c_points = line_2d.Points;
            c_points[0] = laser_start;
            c_points[1] = laser_start;
            line_2d.Points = c_points;

            casting_particles.Position = laser_start;

            Appear();
        }
    }

    private void Appear()
    {
        line_2d.Visible = true;
        if (tween != null && tween.IsRunning())
        {
            tween.Kill();
        }
        // 创建动画，逐渐变粗
        tween = CreateTween();
        tween.TweenProperty(line_2d, "width", line_2d.Width, growth_time * 2.0f).From(0.0f);
    }

    private void Disappear()
    {
        if (tween != null && tween.IsRunning())
        {
            tween.Kill();
        }
        // 创建动画，逐渐消失
        tween = CreateTween();
        tween.TweenProperty(line_2d, "width", 0.0f, growth_time).FromCurrent();
        tween.TweenCallback(Callable.From(line_2d.Hide));
    }
}
