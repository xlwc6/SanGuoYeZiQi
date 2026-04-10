using Godot;
using System;
using System.Linq;

public partial class CardDiscardScene : Control
{
    private const int disOperationTick = 15; // 每回合的时间，秒

    private SelectorGridPanel disCardSelector;
    private Label tpLabel;
    private Label cdLabel;
    private Timer cdTimer;
    private Button disCardBtn;

    private int currentOperationTick = 0; // 当前回合耗时

    public override void _Ready()
    {
        disCardSelector = GetNode<SelectorGridPanel>("%弃牌选择器");
        tpLabel = GetNode<Label>("%弃牌提示");
        cdLabel = GetNode<Label>("%弃牌倒计时");
        cdTimer = GetNode<Timer>("%选择计时器");
        disCardBtn = GetNode<Button>("%确认丢弃");

        VisibilityChanged += CardDiscardScene_VisibilityChanged;
        cdTimer.Timeout += CdTimer_Timeout;
        disCardBtn.Pressed += DisCardBtn_Pressed;
        disCardSelector.Connect(SelectorGridPanel.SignalName.SelectDone, Callable.From<bool>(SelectDoned));

        disCardBtn.Disabled = true;
    }

    private void CardDiscardScene_VisibilityChanged()
    {
        if (!Visible)
        {
            disCardSelector.Clear();
            currentOperationTick = 0;
            cdLabel.Text = $"倒计时：{disOperationTick}";
        }
    }

    private void CdTimer_Timeout()
    {
        currentOperationTick++;
        int minus = disOperationTick - currentOperationTick;
        cdLabel.Text = $"倒计时：{minus}";
        if (currentOperationTick >= disOperationTick)
        {
            // 随机抽取目标数量的牌丢弃
            disCardSelector.RandomSelectCard();
            HidePanel();
        }
    }

    private void DisCardBtn_Pressed()
    {
        HidePanel();
    }

    private void SelectDoned(bool flag)
    {
        disCardBtn.Disabled = !flag;
    }

    /// <summary>
    /// 根据卡牌容器显示弃牌列表
    /// </summary>
    /// <param name="deck">牌堆</param>
    /// <param name="num">需要选择数量</param>
    public void ShowPanelByDeck(IDeck deck, int num)
    {
        disCardSelector.SetCards(deck);
        disCardSelector.MaxSelect = num;
        tpLabel.Text = $"手牌已满，您需要丢弃{num}张";
        Show();
        cdTimer.Start();
    }

    /// <summary>
    /// 隐藏弃牌列表并初始化参数
    /// </summary>
    public void HidePanel()
    {
        // Godot的事件不支持对象数组，改为字符串数组吧
        string[] result = disCardSelector.SelectedCards.Select(x => x.Name).ToArray();
        // 不管是联机还是单机，需要显示选择页面的肯定只有自己
        GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.CardDiscarded, (int)OwnerType.Player, result);
        cdTimer.Stop();
        Hide();
    }
}
