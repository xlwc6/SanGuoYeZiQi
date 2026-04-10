using Godot;
using Godot.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class GameSettingScene : Control
{
    private SoundManager soundManager;
    private Button okBtn;
    private Button canelBtn;
    // 用户设置
    private AvatarRect avatarRect;
    private TextEdit pId;
    private TextEdit pName;
    private TextureButton randomBtn;
    private PopupPanel avatarPop;
    private HFlowContainer avatarList;
    // 音频控制
    private HSlider masterSlider;
    private HSlider bgmSlider;
    private HSlider sfxSlider;

    private string avatar_folder = "res://accests/avatars/";
    private string avatar_scene = "res://ui/avatar_rect.tscn";
    private string date_path = "user://player_info.dat";
    private string config_path = "user://config.ini";
    private bool isModifyPlayer = false;
    private bool isModifyAudio = false;
    private PlayerInfo playerInfo;
    private GameAudioSetting audioSetting;
    private List<string> avatars = [];

    public override void _Ready()
    {
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        okBtn = GetNode<Button>("%确定");
        canelBtn = GetNode<Button>("%取消");
        avatarRect = GetNode<AvatarRect>("%头像");
        pId = GetNode<TextEdit>("%用户编号");
        pName = GetNode<TextEdit>("%用户姓名");
        randomBtn = GetNode<TextureButton>("%随机");
        avatarPop = GetNode<PopupPanel>("%头像选择弹窗");
        avatarList = GetNode<HFlowContainer>("%头像列表");
        masterSlider = GetNode<HSlider>("%总音乐滑块");
        bgmSlider = GetNode<HSlider>("%音乐滑块");
        sfxSlider = GetNode<HSlider>("%音效滑块");

        VisibilityChanged += GameSettingScene_VisibilityChanged;
        okBtn.Pressed += OkBtn_Pressed;
        canelBtn.Pressed += CanelBtn_Pressed;
        randomBtn.Pressed += RandomBtn_Pressed;
        avatarPop.VisibilityChanged += AvatarPop_VisibilityChanged;
        pName.TextChanged += PName_TextChanged;
        masterSlider.ValueChanged += MasterSlider_ValueChanged;
        sfxSlider.ValueChanged += SFXSlider_ValueChanged;
        bgmSlider.ValueChanged += BGMSlider_ValueChanged;

        avatarRect.Connect(AvatarRect.SignalName.Clicked, Callable.From<string>(AvatarClicked));

        soundManager.SetupUISounds(okBtn);
        soundManager.SetupUISounds(canelBtn);
        soundManager.SetupUISounds(masterSlider);
        soundManager.SetupUISounds(bgmSlider);
        soundManager.SetupUISounds(sfxSlider);
    }

    private void GameSettingScene_VisibilityChanged()
    {
        if (Visible)
        {
            LoadPlayerData();
            LoadAudiSetting();
            isModifyPlayer = false;
            isModifyPlayer = false;
        }
    }

    private void OkBtn_Pressed()
    {
        Hide();

        if (isModifyPlayer)
        {
            if(!string.IsNullOrWhiteSpace(pName.Text))
            {
                playerInfo.Name = pName.Text;
            }
            SavePlayerData(playerInfo);
        }
        if (isModifyAudio)
        {
            SaveAudioConfig();
        }
    }

    private void CanelBtn_Pressed()
    {
        Hide();
    }

    private void RandomBtn_Pressed()
    {
        soundManager.PlaySFX("投掷");
        string cName = RandomChineseNameGenerator.Generate();
        pName.Text = cName;
        isModifyPlayer = true;
    }

    private void AvatarPop_VisibilityChanged()
    {
        if (avatarPop.Visible)
        {
            // 重新加载图片列表
            if (avatars.Count == 0) return;
            foreach (var item in avatars)
            {
                var texture = ResourceLoader.Load<PackedScene>(avatar_scene).Instantiate() as AvatarRect;
                avatarList.AddChild(texture);
                texture.SetTexture(item, avatar_folder + item);
                if (item == avatarRect.AvatarName)
                {
                    texture.SetTips("当前选择");
                }
                texture.Connect(AvatarRect.SignalName.Clicked, Callable.From<string>(AvatarSelected));
            }
        }
        else
        {
            // 释放所有资源
            foreach (var item in avatarList.GetChildren())
            {
                item.QueueFree();
            }
        }
    }

    #region 用户设置

    private void LoadPlayerData()
    {
        playerInfo = GetPlayerLocalData();
        // 显示玩家数据
        avatarRect.SetTexture(playerInfo.Avatar, avatar_folder + playerInfo.Avatar);
        pId.Text = playerInfo.PID;
        pName.Text = playerInfo.Name;
        // 获取全部头像
        var all_files = ResourceLoader.ListDirectory(avatar_folder);
        foreach (var file in all_files)
        {
            if (file.EndsWith(".jpg"))
            {
                avatars.Add(file);
            }
        }
    }

    private void SavePlayerData(PlayerInfo info)
    {
        var dataForSave = new Godot.Collections.Dictionary<string, Variant>
        {
            ["PID"] = info.PID,
            ["Name"] = info.Name,
            ["Avatar"] = info.Avatar
        };
        using var savedFileForWrite = FileAccess.Open(date_path, FileAccess.ModeFlags.Write);
        savedFileForWrite.StoreVar(dataForSave, true);
    }

    private PlayerInfo GetPlayerLocalData()
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
        var _info = new PlayerInfo
        {
            PID = pId,
            Name = pName,
            Avatar = "c60a2ddbb6fd5266836ae001ed18972bd50736a7.jpg"
        };
        SavePlayerData(_info);
        // 返回默认的
        return _info;
    }

    private void AvatarClicked(string name)
    {
        if (avatarPop.Visible) return;
        // 打开头像选择框
        avatarPop.Show();
        isModifyPlayer = true;
    }

    private void AvatarSelected(string name)
    {
        avatarRect.SetTexture(name, avatar_folder + name);
        playerInfo.Avatar = name;

        avatarPop.Hide();
    }

    private void PName_TextChanged()
    {
        if (pName.Text != playerInfo.Name)
        {
            isModifyPlayer = true;
        }
    }

    #endregion

    #region 音频设置

    private void LoadAudiSetting()
    {
        audioSetting = GetLocalAudiSetting();

        masterSlider.Value = audioSetting.masterSize;
        bgmSlider.Value = audioSetting.bgmSize;
        sfxSlider.Value = audioSetting.sfxSize;
    }

    private void SaveAudioConfig()
    {
        soundManager.SetVolume(BusType.Master, audioSetting.masterSize);
        soundManager.SetVolume(BusType.BGM, audioSetting.bgmSize);
        soundManager.SetVolume(BusType.SFX, audioSetting.sfxSize);

        using var config = new ConfigFile();

        config.SetValue("Audio", "Master", audioSetting.masterSize);
        config.SetValue("Audio", "SFX", audioSetting.sfxSize);
        config.SetValue("Audio", "BGM", audioSetting.bgmSize);

        config.Save(config_path);
    }

    private GameAudioSetting GetLocalAudiSetting()
    {
        GameAudioSetting setting = new GameAudioSetting();

        using var config = new ConfigFile();
        config.Load(config_path);

        setting.masterSize = config.GetValue("Audio", "Master", 0.5f).AsSingle();
        setting.sfxSize = config.GetValue("Audio", "SFX", 1.0f).AsSingle();
        setting.bgmSize = config.GetValue("Audio", "BGM", 1.0f).AsSingle();

        return setting;
    }

    private void MasterSlider_ValueChanged(double value)
    {
        if (value != audioSetting.masterSize)
        {
            audioSetting.masterSize = (float)value;
            isModifyAudio = true;
        }
    }

    private void SFXSlider_ValueChanged(double value)
    {
        if (value != audioSetting.sfxSize)
        {
            audioSetting.sfxSize = (float)value;
            isModifyAudio = true;
        }
    }

    private void BGMSlider_ValueChanged(double value)
    {
        if (value != audioSetting.bgmSize)
        {
            audioSetting.bgmSize = (float)value;
            isModifyAudio = true;
        }
    }

    #endregion
}
