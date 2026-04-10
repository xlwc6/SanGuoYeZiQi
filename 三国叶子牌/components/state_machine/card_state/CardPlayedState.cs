using Godot;
using System;

public partial class CardPlayedState : BaseState
{
    public override void OnStateEnter()
    {
        card.ZIndex = 0;
        card.ChangedStyleBox("base");
        GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.CardPlayed, card);
    }
}
