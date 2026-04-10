using Godot;
using System;

public partial class UdpServerNode : Node
{
    private const int udpPort = 26010; // UDP监听端口

    private UdpServer _server = new UdpServer();

    public override void _Ready()
    {
        if(IsMultiplayerAuthority())
        {
            _server.Listen(udpPort);
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        _server.Poll(); // 重要！
        if (_server.IsConnectionAvailable())
        {
            PacketPeerUdp peer = _server.TakeConnection();
            var packet = peer.GetPacket();

            GD.Print($"接受对等体：{peer.GetPacketIP()}:{peer.GetPacketPort()}");
            GD.Print($"接收到数据：{packet.GetStringFromUtf8()}");

            // 回复房间信息
            var data_to_send = new Godot.Collections.Array<string>
            {
                AppGlobalData.RoomName, // 房间名称
                AppGlobalData.ServerIP, // 服务器IP
                AppGlobalData.ServerPort.ToString(), // 多人游戏端口
                AppGlobalData.RoomMaxNum.ToString(), // 最大人数
                AppGlobalData.ConnectNum.ToString(), // 已连接人数
                AppGlobalData.Password // 密码
            };

            var json_string = Json.Stringify(data_to_send);
            peer.PutPacket(json_string.ToUtf8Buffer());
        }
    }
}
