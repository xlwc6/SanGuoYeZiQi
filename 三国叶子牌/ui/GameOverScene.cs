using Godot;
using System;

public partial class GameOverScene : Control
{
    private GameEvent gameEvent;
    private SoundManager soundManager;
    private Label endLabel;
    private Button restartBtn;
    private Button exitBtn;

    private string transitionScene = "res://ui/transition.tscn";
    private string singleGameScene = "res://ui/game_panel.tscn";
    private bool isSiglePlayer = true;

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        endLabel = GetNode<Label>("%提示信息");
        restartBtn = GetNode<Button>("%重新开始");
        exitBtn = GetNode<Button>("%结束游戏");

        restartBtn.Pressed += StartGame;
        exitBtn.Pressed += QuitGame;

        isSiglePlayer = Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer;
        if (!isSiglePlayer)
        {
            restartBtn.Text = "继续游戏";
        }
        restartBtn.GrabFocus();

        soundManager.SetupUISounds(restartBtn);
        soundManager.SetupUISounds(exitBtn);
    }

    public void SetInfo(string text)
    {
        endLabel.Text = text;
    }

    private async void StartGame()
    {
        var tranNode = ResourceLoader.Load<PackedScene>(transitionScene).Instantiate() as Transition;
        GetTree().Root.AddChild(tranNode);

        await tranNode.FadeInAsync();

        if (isSiglePlayer)
        {
            GetTree().ChangeSceneToFile(singleGameScene);
        }
        else
        {
            Rpc(MethodName.RequestContinueGame, Multiplayer.GetUniqueId());
        }

        await tranNode.FadeOutAsync();

        tranNode.QueueFree();
    }

    private void QuitGame()
    {
        if (isSiglePlayer)
        {
            GetTree().Quit();
        }
        else
        {
            gameEvent.EmitSignal(GameEvent.SignalName.QuitGameRequested);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RequestContinueGame(int peerId)
    {
        int sendId = Multiplayer.GetRemoteSenderId();
        gameEvent.EmitSignal(GameEvent.SignalName.ContinueGame, sendId);
        if (sendId == Multiplayer.GetUniqueId())
        {
            Visible = false;
        }
    }
}
