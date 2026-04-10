using System;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// 应用全局变量
/// </summary>
public class AppGlobalData
{
    /// <summary>
    /// 服务器IP
    /// </summary>
    public static string ServerIP { get; set; }
    /// <summary>
    /// 服务器IP
    /// </summary>
    public static int ServerPort { get; set; }
    /// <summary>
    /// 房间名称
    /// </summary>
    public static string RoomName { get; set; }
    /// <summary>
    /// 房间最大人数
    /// </summary>
    public static int RoomMaxNum { get; set; }
    /// <summary>
    /// 房间连接人数
    /// </summary>
    public static int ConnectNum { get; set; }
    /// <summary>
    /// 房间密码
    /// </summary>
    public static string Password { get; set; }

    /// <summary>
    /// 重置房间数据
    /// </summary>
    public static void InitRoomData()
    {
        ServerIP = null;
        ServerPort = 0;
        RoomName = null;
        RoomMaxNum = 0;
        ConnectNum = 0;
        Password = null;
    }

    /// <summary>
    /// 获取本地IP
    /// </summary>
    /// <returns></returns>
    public static string GetLocalIPAddress()
    {
        try
        {
            // 获取本机所有网络接口
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // 排除回环地址
                    if (!IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }

            // 如果没有找到其他IP，使用回环地址
            return "127.0.0.1";
        }
        catch (Exception)
        {
            return "127.0.0.1";
        }
    }
}
