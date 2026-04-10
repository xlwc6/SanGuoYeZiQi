using Godot;
using Godot.Collections;
using System;
using System.Security.Cryptography;

/// <summary>
/// 玩家界面
/// </summary>
public partial class PlayerCanvasLayer : Control
{
    public PlayerInfo PlayerInfo;

    private TextureRect avatar;
    private Label name;

    private string avatar_folder = "res://accests/avatars/";
    private string date_path = "user://player_info.dat";

    public override void _Ready()
    {
        avatar = GetNode<TextureRect>("%图片");
        name = GetNode<Label>("%昵称");

        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        // 方便测试，给客服端改个名字
        if(!IsMultiplayerAuthority())
        {
            PlayerInfo = new PlayerInfo
            {
                PID = "20260403",
                Name = "张三",
                Avatar = "2b40940a19d8bc3e52c91807c78ba61eaad3457f.jpg",
            };
        }
        else
        {
            PlayerInfo = GetLocalData();
        }

        avatar.Texture = GD.Load<Texture2D>(avatar_folder + PlayerInfo.Avatar);
        name.Text = PlayerInfo.Name;
    }

    private void SetLocalData(PlayerInfo info)
    {
        var dataForSave = new Dictionary<string, Variant>
        {
            ["PID"] = info.PID,
            ["Name"] = info.Name,
            ["Avatar"] = info.Avatar
        };
        using var savedFileForWrite = FileAccess.Open(date_path, FileAccess.ModeFlags.Write);
        savedFileForWrite.StoreVar(dataForSave, true);
    }

    private PlayerInfo GetLocalData()
    {
        if (FileAccess.FileExists(date_path))
        {
            using var savedFileForRead = FileAccess.Open(date_path, FileAccess.ModeFlags.Read);
            if (savedFileForRead.GetLength() > 0)
            {
                var dataForSave = savedFileForRead.GetVar(true).AsGodotDictionary<string, Variant>();
                if (dataForSave != null)
                {
                    return new PlayerInfo
                    {
                        PID = dataForSave["PID"].AsString(),
                        Name = dataForSave["Name"].AsString(),
                        Avatar = dataForSave["Avatar"].AsString(),
                    };
                }
            }
        }
        string pId = RandomChineseNameGenerator.GenerateSimple();
        string pName = RandomChineseNameGenerator.GenerateMale();
        // 保存默认数据
        var playerInfo = new PlayerInfo
        {
            PID = pId,
            Name = pName,
            Avatar = "c60a2ddbb6fd5266836ae001ed18972bd50736a7.jpg"
        };
        SetLocalData(playerInfo);
        // 返回默认的
        return playerInfo;
    }
}
