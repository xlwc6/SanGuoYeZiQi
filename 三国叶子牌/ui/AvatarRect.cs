using Godot;
using System;

public partial class AvatarRect : TextureRect
{
    /// <summary>
    /// 鼠标左键点击事件
    /// </summary>
    [Signal]
    public delegate void ClickedEventHandler(string name);

    /// <summary>
    /// 头像名
    /// </summary>
    [Export]
    public string AvatarName;

    private Label tips;

    public override void _Ready()
    {
        tips = GetNode<Label>("提示信息");
        tips.Visible = false;
    }

    public override void _GuiInput(InputEvent @event)
    {
        // 鼠标左键点击
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
        {
            EmitSignal(SignalName.Clicked, AvatarName);
        }
    }

    public void SetTexture(string avatar, string path)
    {
        AvatarName = avatar;
        Texture = GD.Load<Texture2D>(path);
    }

    public void SetTips(string msg)
    {
        tips.Text = msg;
        tips.Visible = true;
    }
}
