using Godot;
using System;

public partial class GamePausedScene : Control
{
    private SoundManager soundManager;
    private RichTextLabel richLabel;
    private Button resumeBtn;
    private Button restartBtn;
    private Button backBtn;

    private string transitionScene = "res://ui/transition.tscn";

    public override void _Ready()
    {
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        richLabel = GetNode<RichTextLabel>("%日志显示标签");
        resumeBtn = GetNode<Button>("%继续游戏");
        restartBtn = GetNode<Button>("%重新开始");
        backBtn = GetNode<Button>("%回到标题");

        resumeBtn.Pressed += ResumeBtnGame;
        restartBtn.Pressed += StartGame;
        backBtn.Pressed += BackGame;

        Hide();

        VisibilityChanged += GamePausedScene_VisibilityChanged;

        soundManager.SetupUISounds(resumeBtn);
        soundManager.SetupUISounds(restartBtn);
        soundManager.SetupUISounds(backBtn);
    }

    public override void _Input(InputEvent @event)
    {
        if(@event.IsActionPressed("ui_cancel"))
        {
            Hide();
            GetWindow().SetInputAsHandled();
        }
    }

    public void PrintGameLogs(string message)
    {
        richLabel.AppendText(message);
        richLabel.Newline();
    }

    public void ClearLogs()
    {
        richLabel.Clear();
    }

    public void ShowPause()
    {
        Show();
        resumeBtn.GrabFocus();
    }

    private void GamePausedScene_VisibilityChanged()
    {
        GetTree().Paused = Visible;
    }

    private void ResumeBtnGame()
    {
        Hide();
    }

    private async void StartGame()
    {
        Hide();

        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.FadeInAsync();

        GetTree().ChangeSceneToFile("res://ui/game_panel.tscn");

        await tranNode.FadeOutAsync();

        tranNode.QueueFree();
    }

    private async void BackGame()
    {
        Hide();

        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.WipeInAsync();

        GetTree().ChangeSceneToFile("res://main.tscn");

        await tranNode.WipeOutAsync();

        tranNode.QueueFree();
    }
}
