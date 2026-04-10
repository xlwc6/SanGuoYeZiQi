using Godot;
using System;
using System.Threading.Tasks;

public partial class CardNormalState : BaseState
{
    public override async void OnStateEnter()
    {
        // 等待渲染完成
        if (!card.IsNodeReady())
        {
            await ToSignal(card, Node.SignalName.Ready);
        }
        card.ZIndex = 0;
        card.ChangedStyleBox("base");
    }

    public override void OnGuiInput(InputEvent e)
    {
        if (card.disabled) return;
        if (e.IsActionPressed("mouse_left"))
        {
            stateMachine.ChangeToState<CardClickedState>();
        }
    }

    public override void OnMouseEnter()
    {
        if (card.disabled) return;
        card.PlayAni("选择");
    }

    public override void OnMouseExit()
    {
        if (card.disabled) return;
        card.PlayAni("取消");
    }
}
