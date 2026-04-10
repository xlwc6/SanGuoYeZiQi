using Godot;
using System;

public partial class SoundManager : Node
{
    private Node sfx;
    private Node bgm;

    private string config_path = "user://config.ini";
    private string currentBgm;

    public override void _Ready()
    {
        sfx = GetNode<Node>("SFX");
        bgm = GetNode<Node>("BGM");

        // 加载音乐设置
        LoadAudiSetting();
    }

    private void LoadAudiSetting()
    {
        using var config = new ConfigFile();
        config.Load(config_path);

        SetVolume(BusType.Master, config.GetValue("Audio", "Master", 0.5f).AsSingle());
        SetVolume(BusType.SFX, config.GetValue("Audio", "SFX", 1.0f).AsSingle());
        SetVolume(BusType.BGM, config.GetValue("Audio", "BGM", 1.0f).AsSingle());
    }

    /// <summary>
    /// 设置BGM
    /// </summary>
    /// <param name="name"></param>
    /// <param name="stream"></param>
    public void SetBGM(string name, AudioStream stream)
    {
        var player = sfx.GetNode<AudioStreamPlayer>(name);
        if (player != null && player.Stream != stream)
        {
            player.Stream = stream;
        }
    }

    /// <summary>
    /// 播放音乐
    /// </summary>
    /// <param name="name"></param>
    public void PlayBGM(string name)
    {
        if (currentBgm == name) return;

        if(!string.IsNullOrEmpty(currentBgm))
        {
            var lastPlayer = bgm.GetNode<AudioStreamPlayer>(currentBgm);
            lastPlayer?.Stop();
        }

        var player = bgm.GetNode<AudioStreamPlayer>(name);
        player?.Play();

        currentBgm = name;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name"></param>
    public void PlaySFX(string name)
    {
        var player = sfx.GetNode<AudioStreamPlayer>(name);
        player?.Play();
    }

    /// <summary>
    /// 获取音量设置
    /// </summary>
    /// <param name="bus"></param>
    /// <returns></returns>
    public float GetVolume(BusType bus)
    {
        var db = AudioServer.GetBusVolumeDb((int)bus);
        return Mathf.DbToLinear(db);
    }

    /// <summary>
    /// 设置音量
    /// </summary>
    /// <param name="bus"></param>
    /// <param name="value"></param>
    public void SetVolume(BusType bus, float value)
    {
        var db = Mathf.LinearToDb(value);
        AudioServer.SetBusVolumeDb((int)bus, db);
    }

    /// <summary>
    /// 设置界面音效
    /// </summary>
    /// <param name="node"></param>
    public void SetupUISounds(Node node)
    {
        if (node is Button btn)
        {
            btn.Connect(Button.SignalName.Pressed, Callable.From(() =>
            {
                PlaySFX("点击");
            }));
            btn.Connect(Button.SignalName.FocusEntered, Callable.From(() =>
            {
                PlaySFX("移入");
            }));
            btn.Connect(Button.SignalName.MouseEntered, Callable.From(btn.GrabFocus));
        }
        else if (node is Slider slider)
        {
            slider.Connect(Slider.SignalName.ValueChanged, Callable.From<double>((value) =>
            {
                PlaySFX("拖动");
            }));
            slider.Connect(Slider.SignalName.FocusEntered, Callable.From(() =>
            {
                PlaySFX("移入");
            }));
            slider.Connect(Slider.SignalName.MouseEntered, Callable.From(slider.GrabFocus));
        }
        // 递归设置子控件
        if (node.GetChildCount() > 0)
        {
            foreach (var item in node.GetChildren())
            {
                SetupUISounds(item);
            }
        }
    }
}
