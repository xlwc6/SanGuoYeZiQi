using Godot;
using System;

public partial class CardClickedState : BaseState
{
    public override void OnStateEnter()
    {
        card.ZIndex = 1;
        card.ChangedStyleBox("clicked");
        GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.CardClicked, card);
    }

    public override void OnGuiInput(InputEvent e)
    {
        if (e.IsActionPressed("mouse_right"))
        {
            stateMachine.ChangeToState<CardNormalState>();
            GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.CardReleased, card);
        }
    }
}
