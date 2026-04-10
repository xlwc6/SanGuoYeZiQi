using Godot;
using System;
using System.Linq;

public partial class Heart : HBoxContainer
{
    [Export]
    private Texture2D heart_full;
    [Export]
    private Texture2D heart_empty;
    [Export]
    private OwnerType ownerType;

    public int HeartCount;
    public int CurrentHeart;

    private GameEvent gameEvent;

    public override void _Ready()
    {
        HeartCount = GetChildCount();
        CurrentHeart = HeartCount;

        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        gameEvent.Connect(GameEvent.SignalName.BeHurt, Callable.From<int, int>(OnBeHurt));
        
        bool isSiglePlayer = Multiplayer.MultiplayerPeer is OfflineMultiplayerPeer;
        if (!isSiglePlayer)
        {
            gameEvent.Connect(GameEvent.SignalName.ContinueGame, Callable.From<int>(OnContinueGame));
        }
    }

    private void OnBeHurt(int owner, int damage)
    {
        if (CurrentHeart <= 0) return;
        if (ownerType == (OwnerType)owner)
        {
            CurrentHeart -= damage;
            UpdateHeart(CurrentHeart);
        }
        if (CurrentHeart <= 0)
        {
            gameEvent.EmitSignal(GameEvent.SignalName.Death, (int)ownerType);
        }
    }

    private void UpdateHeart(int value)
    {
        for (int i = 0; i < GetChildCount(); i++)
        {
            if (i < value)
            {
                GetChild<TextureRect>(i).Texture = heart_full;
            }
            else
            {
                GetChild<TextureRect>(i).Texture = heart_empty;
            }
        }
    }

    private void OnContinueGame(int peerId)
    {
        if (peerId == Multiplayer.GetUniqueId())
        {
            CurrentHeart = HeartCount;
            UpdateHeart(HeartCount);
        }
    }
}
