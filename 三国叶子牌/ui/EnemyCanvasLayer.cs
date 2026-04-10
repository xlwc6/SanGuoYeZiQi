using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 敌人界面
/// </summary>
public partial class EnemyCanvasLayer : Control
{
    public PlayerInfo EnemyInfo;

    private TextureRect avatar;
    private Label name;

    private string avatar_folder = "res://accests/avatars/";

    public override void _Ready()
    {
        avatar = GetNode<TextureRect>("%图片");
        name = GetNode<Label>("%昵称");

        if (EnemyInfo == null)
        {
            GetRandmoInfo();
        }
    }

    public void UpdatePlatInfo(PlayerInfo info)
    {
        EnemyInfo = info;

        avatar.Texture = GD.Load<Texture2D>(avatar_folder + EnemyInfo.Avatar);
        name.Text = EnemyInfo.Name;
    }

    private void GetRandmoInfo()
    {
        string pId = RandomChineseNameGenerator.GenerateSimple();
        string pName = RandomChineseNameGenerator.Generate();
        string pAvatar = GetRandomAvatar();

        EnemyInfo = new PlayerInfo
        {
            PID = pId,
            Name = pName,
            Avatar = pAvatar,
        };

        avatar.Texture = GD.Load<Texture2D>(avatar_folder + pAvatar);
        name.Text = pName;
    }

    private string GetRandomAvatar()
    {
        // 随机获取一张头像
        List<string> lists = new List<string>();

        var all_files = ResourceLoader.ListDirectory(avatar_folder);
        foreach (var file in all_files)
        {
            if (file.EndsWith(".jpg"))
            {
                lists.Add(file);
            }
        }
        int index = GD.RandRange(0, lists.Count - 1);
        return lists[index];
    }
}
