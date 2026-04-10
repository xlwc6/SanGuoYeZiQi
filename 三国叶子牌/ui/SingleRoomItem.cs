using Godot;
using System;

public partial class SingleRoomItem : Button
{
    /// <summary>
    /// 选择事件
    /// </summary>
    [Signal]
    public delegate void SelectedEventHandler(int index, Node node);

    /// <summary>
    /// 服务器IP
    /// </summary>
    public string ServerIP { get; set; }
    /// <summary>
    /// 服务器IP
    /// </summary>
    public int ServerPort { get; set; }
    /// <summary>
    /// 房间名称
    /// </summary>
    public string RoomName { get; set; }
    /// <summary>
    /// 房间最大人数
    /// </summary>
    public int RoomMaxNum { get; set; }
    /// <summary>
    /// 房间连接人数
    /// </summary>
    public int ConnectNum { get; set; }
    /// <summary>
    /// 房间密码
    /// </summary>
    public string Password { get; set; }

    private Label roomNameLbl;
    private Label methodLbl;
    private Label passwordLbl;
    private Label countLbl;

    public override void _Ready()
    {
        roomNameLbl = GetNode<Label>("%房间名称");
        methodLbl = GetNode<Label>("%游戏模式");
        passwordLbl = GetNode<Label>("%密码设置");
        countLbl = GetNode<Label>("%房间人数");

        Toggled += SingleRoomItem_Toggled;

        roomNameLbl.Text = RoomName;
        methodLbl.Text = "普通模式";
        passwordLbl.Text = string.IsNullOrEmpty(Password) ? "无需密码" : "需要密码";
        countLbl.Text = $"{ConnectNum} / {RoomMaxNum}";
    }

    private void SingleRoomItem_Toggled(bool toggledOn)
    {
        int index = GetIndex();
        if (!toggledOn)
        {
            index = -1;
        }
        EmitSignal(SignalName.Selected, index, this);
    }
}
