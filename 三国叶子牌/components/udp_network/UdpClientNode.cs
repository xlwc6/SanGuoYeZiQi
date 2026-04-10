using Godot;
using System;

public partial class UdpClientNode : Node
{
    /// <summary>
    /// 搜索到数据
    /// </summary>
    [Signal]
    public delegate void SearchResultEventHandler(string roomInfo);

    private const int udpServerPort = 26010; // UDP监听端口
    private const int udpClientPort = 26012; // UDP发送数据端口

    private PacketPeerUdp _udp = new PacketPeerUdp(); // UDP 数据包客户端

    public override void _Ready()
    {
        _udp.Bind(udpClientPort);
        // 启用广播方式
        _udp.SetBroadcastEnabled(true);
        _udp.SetDestAddress("255.255.255.255", udpServerPort);
    }

    public override void _Process(double delta)
    {
        if (_udp.GetAvailablePacketCount() > 0)
        {
            var array_bytes = _udp.GetPacket();
            var packet_string = array_bytes.GetStringFromUtf8();

            GD.Print($"接收到数据：{packet_string}");

            EmitSignalSearchResult(packet_string);
        }
    }

    public void Search()
    {
        _udp.PutPacket("SEARCH".ToUtf8Buffer());

        GD.Print("发送搜索数据：SEARCH");
    }
}
