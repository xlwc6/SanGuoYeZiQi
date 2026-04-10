using Godot;
using System;
using System.Collections.Generic;

public partial class Lobby : Control
{
    private const int PORT = 9610; // 这里我习惯使用固定端口，

    private const string mainScene = "res://main.tscn";
    private const string pvpRoomScene = "res://ui/pvp_game_room.tscn";
    private const string singleRoomScene = "res://ui/single_room_item.tscn";

    private UdpClientNode clientNode;
    private GridContainer roomGrids;
    private TextEdit seachText;
    private Button searchBtn;
    private TextEdit roomNameText;
    private TextEdit passwordText;
    private CheckButton passwordBtn;
    private Button hostBtn;
    private Button joinBtn;
    private Button backBtn;
    private MessageBox messageBox;

    private List<string> roomList = [];
    private int selectIndex = -1; // 选择的列表索引
    private bool isConnecting; // 正在连接

    public override void _Ready()
    {
        clientNode = GetNode<UdpClientNode>("局域网客户端");
        roomGrids = GetNode<GridContainer>("%房间列表表格");
        seachText = GetNode<TextEdit>("%搜索房间");
        searchBtn = GetNode<Button>("%搜索按钮");
        roomNameText = GetNode<TextEdit>("%房间名称");
        passwordText = GetNode<TextEdit>("%密码");
        passwordBtn = GetNode<CheckButton>("%启用密码");
        hostBtn = GetNode<Button>("%创建");
        joinBtn = GetNode<Button>("%加入");
        backBtn = GetNode<Button>("%回到标题");
        messageBox = GetNode<MessageBox>("%消息弹窗");

        searchBtn.Pressed += SearchBtn_Pressed;
        hostBtn.Pressed += HostBtn_Pressed;
        joinBtn.Pressed += JoinBtn_Pressed;
        backBtn.Pressed += BackBtn_Pressed;
        passwordBtn.Toggled += PasswordBtn_Toggled;

        Multiplayer.ConnectedToServer += OnConnectOk;
        Multiplayer.ConnectionFailed += OnConnectionFail;

        clientNode.Connect(UdpClientNode.SignalName.SearchResult, Callable.From<string>(OnSearchResult));

        clientNode.Search();

        hostBtn.Disabled = false;
        joinBtn.Disabled = true;
        messageBox.Visible = false;
    }

    private void OnSearchResult(string roomInfo)
    {
        // 解析返回数据
        var parts = Json.ParseString(roomInfo).AsGodotArray<string>();
        if (parts.Count == 6)
        {
            string roomName = parts[0]; // 房间名称
            string serverIP = parts[1]; // 服务器IP
            int eNetPort = int.Parse(parts[2]); // 多人游戏端口
            int maxCount = int.Parse(parts[3]); // 最大人数
            int peerCount = int.Parse(parts[4]);// 已连接人数
            string password = parts[5];// 密码

            // 搜索过滤
            if (!string.IsNullOrEmpty(seachText.Text) && !roomName.Contains(seachText.Text))
            {
                return;
            }

            // 避免重复添加
            if (roomList.Contains(roomName)) return;

            var roomItem = ResourceLoader.Load<PackedScene>(singleRoomScene).Instantiate() as SingleRoomItem;
            roomItem.RoomName = roomName;
            roomItem.ServerIP = serverIP;
            roomItem.ServerPort = eNetPort;
            roomItem.RoomMaxNum = maxCount;
            roomItem.ConnectNum = peerCount;
            roomItem.Password = password;
            roomGrids.AddChild(roomItem);
            roomItem.Connect(SingleRoomItem.SignalName.Selected, Callable.From<int, Node>(OnSingleRoomItemSelected));

            roomList.Add(roomName);
        }
    }

    private void SearchBtn_Pressed()
    {
        // 清空房间列表
        roomList.Clear();
        foreach (var item in roomGrids.GetChildren())
        {
            item.QueueFree();
        }

        clientNode.Search();
    }

    private void PasswordBtn_Toggled(bool toggledOn)
    {
        passwordText.Editable = toggledOn;
        if (!toggledOn)
        {
            passwordText.Text = "";
        }
    }

    private void HostBtn_Pressed()
    {
        var error = CreateGame();
        if (error != Error.Ok)
        {
            //GD.Print("创建服务器失败，错误代码：" + error);
            messageBox.ShowMessage("创建服务器失败，错误代码：" + error);
            return;
        }
        // 记录服务器信息
        AppGlobalData.ServerIP = AppGlobalData.GetLocalIPAddress();
        AppGlobalData.ServerPort = PORT;
        AppGlobalData.RoomMaxNum = 2;
        AppGlobalData.ConnectNum = 1;
        AppGlobalData.RoomName = roomNameText.Text;
        AppGlobalData.Password = passwordText.Text;

        GetTree().ChangeSceneToFile(pvpRoomScene);
    }

    private void JoinBtn_Pressed()
    {
        if (selectIndex < 0) return;
        if (!string.IsNullOrEmpty(AppGlobalData.Password) && AppGlobalData.Password != passwordText.Text)
        {
            passwordText.GrabFocus();
            messageBox.ShowMessage("密码错误！");
            return;
        }

        var error = JoinGame(AppGlobalData.ServerIP);
        if (error != Error.Ok)
        {
            GD.Print("加入房间失败，错误代码：" + error);
            messageBox.ShowMessage("加入房间失败，错误代码：" + error);
        }
    }

    private void BackBtn_Pressed()
    {
        GetTree().ChangeSceneToFile(mainScene);
    }

    private void OnSingleRoomItemSelected(int index, Node node)
    {
        selectIndex = index;
        hostBtn.Disabled = false;
        joinBtn.Disabled = true;
        roomNameText.Text = "";
        roomNameText.Editable = true;
        passwordText.Text = "";
        if (selectIndex >= 0)
        {
            var roomItem = node as SingleRoomItem;
            roomNameText.Text = roomItem.RoomName;
            roomNameText.Editable = false;
            hostBtn.Disabled = true;
            if (roomItem.ConnectNum < roomItem.RoomMaxNum)
            {
                joinBtn.Disabled = false;

                // 记录服务器信息
                AppGlobalData.ServerIP = roomItem.ServerIP;
                AppGlobalData.ServerPort = roomItem.ServerPort;
                AppGlobalData.RoomMaxNum = roomItem.RoomMaxNum;
                AppGlobalData.ConnectNum = roomItem.ConnectNum;
                AppGlobalData.Password = roomItem.Password;
            }
        }
    }

    private void OnConnectOk()
    {
        GetTree().ChangeSceneToFile(pvpRoomScene);
    }

    private void OnConnectionFail()
    {
        isConnecting = false;
        GD.Print("连接服务器失败！");
        messageBox.ShowMessage("连接服务器失败！");
    }


    private Error CreateGame()
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(PORT, 1);

        if (error != Error.Ok)
        {
            return error;
        }

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    private Error JoinGame(string ip)
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(ip, PORT);

        if (error != Error.Ok)
        {
            return error;
        }
        isConnecting = true;
        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }
}
