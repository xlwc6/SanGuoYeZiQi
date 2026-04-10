using Godot;
using System;

public partial class CardDiscardState : BaseState
{
    public override void OnStateEnter()
    {
        card.SetOcclusion(true);
    }

    public override void OnStateExit()
    {
        card.SetOcclusion(false);
    }
}
