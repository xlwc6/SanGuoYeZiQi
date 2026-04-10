using Godot;
using System;

public partial class Main : Node
{
    private SoundManager soundManager;
    private Button startBtn;
    private Button pvpBtn;
    private Button gameInfoBtn;
    private Button gameSettingBtn;
    private Button quitBtn;
    private PopupPanel popupPanel;
    private RichTextLabel richText;
    private GameSettingScene gameSetting;

    private string transitionScene = "res://ui/transition.tscn";
    private string singleGameScene = "res://ui/game_panel.tscn";
    private string lobbyScene = "res://ui/lobby.tscn";
    private string gameInfoPath = "res://resources/others/game_info.txt";

    public override void _Ready()
    {
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        startBtn = GetNode<Button>("%开始游戏");
        pvpBtn = GetNode<Button>("%联机对战");
        gameInfoBtn = GetNode<Button>("%游戏说明");
        gameSettingBtn = GetNode<Button>("%游戏设置");
        quitBtn = GetNode<Button>("%退出游戏");
        popupPanel = GetNode<PopupPanel>("%游戏说明弹窗");
        richText = GetNode<RichTextLabel>("%文本标签");
        gameSetting = GetNode<GameSettingScene>("%游戏设置页面");

        startBtn.Pressed += StartBtn_Pressed;
        pvpBtn.Pressed += PvpBtn_Pressed;
        gameInfoBtn.Pressed += GameInfoBtn_Pressed;
        gameSettingBtn.Pressed += GameSettingBtn_Pressed;
        quitBtn.Pressed += QuitBtn_Pressed;

        LoadGameInfo();

        startBtn.GrabFocus();

        soundManager.PlayBGM("首页");
        soundManager.SetupUISounds(startBtn);
        soundManager.SetupUISounds(gameInfoBtn);
        soundManager.SetupUISounds(gameSettingBtn);
        soundManager.SetupUISounds(quitBtn);
    }

    private async void StartBtn_Pressed()
    {
        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.WipeInAsync();

        GetTree().ChangeSceneToFile(singleGameScene);

        await tranNode.WipeOutAsync();

        tranNode.QueueFree();
    }

    private async void PvpBtn_Pressed()
    {
        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.WipeInAsync();

        GetTree().ChangeSceneToFile(lobbyScene);

        await tranNode.WipeOutAsync();

        tranNode.QueueFree();
    }

    private void GameInfoBtn_Pressed()
    {
        popupPanel.Show();
    }

    private void GameSettingBtn_Pressed()
    {
        gameSetting.Show();
    }

    private void QuitBtn_Pressed()
    {
        GetTree().Quit();
    }

    private void LoadGameInfo()
    {
        if (!FileAccess.FileExists(gameInfoPath))
        {
            return;
        }

        using var file = FileAccess.Open(gameInfoPath, FileAccess.ModeFlags.Read);
        var gameInfoStr = file.GetAsText();

        richText.Text = gameInfoStr;
    }
}
