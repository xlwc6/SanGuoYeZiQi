using Godot;
using System;

/// <summary>
/// 操作面板
/// </summary>
public partial class OperationBar : Panel
{
    private GameEvent gameEvent;
    private SoundManager soundManager;
    private Button cfmBtn;

    private Card playingCard;
    private bool isCanPlay = false;
    private int skipRoundCount = 0;

    public override void _Ready()
    {
        gameEvent = GetNode<GameEvent>("/root/GameEvent");
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        cfmBtn = GetNode<Button>("ConfirmBtn");

        Hide();

        // 连接卡牌信号
        gameEvent.Connect(GameEvent.SignalName.CardClicked, Callable.From<Card>(OnCardClicked));
        gameEvent.Connect(GameEvent.SignalName.CardReleased, Callable.From<Card>(OnCardReleased));
        gameEvent.Connect(GameEvent.SignalName.TurnBegin, Callable.From(OnTurnBegin));
        gameEvent.Connect(GameEvent.SignalName.TurnEnd, Callable.From(OnTurnEnd));
        gameEvent.Connect(GameEvent.SignalName.TurnSkipNext, Callable.From(OnTurnSkipNext));

        cfmBtn.Pressed += CfmBtn_Pressed;

        soundManager.SetupUISounds(cfmBtn);
    }

    private void OnTurnBegin()
    {
        if (skipRoundCount > 0)
        {
            skipRoundCount--;
            isCanPlay = false;
            return;
        }
        isCanPlay = true;
    }

    private void OnTurnEnd()
    {
        isCanPlay = false;
    }

    private void OnTurnSkipNext()
    {
        skipRoundCount++;
    }

    private void OnCardClicked(Card card)
    {
        if (!isCanPlay) return;
        if(card.ownerType== OwnerType.Player)
        {
            playingCard = card;
            Show();
        }
    }

    private void OnCardReleased(Card card)
    {
        if (card.ownerType == OwnerType.Player)
        {
            playingCard = null;
            Hide();
        }
    }

    private void CfmBtn_Pressed()
    {
        if (!isCanPlay) return;
        isCanPlay = false;
        Hide();
        playingCard.ChangedToState(BaseState.State.Played);
    }
}
