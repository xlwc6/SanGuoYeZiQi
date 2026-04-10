using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

/// <summary>
/// 卡牌
/// </summary>
public partial class Card : Control
{
    [Export]
    public CardData cardData;

    /// <summary>
    /// 卡牌拥有者
    /// </summary>
    public OwnerType ownerType;
    /// <summary>
    /// 卡牌索引,相对于卡牌管理器
    /// </summary>
    public int recordIndex;
    /// <summary>
    /// 禁用，无法进行交互
    /// </summary>
    public bool disabled = false;

    private SoundManager soundManager;
    private PanelContainer visual;
    private Panel panel;
    private Panel backPanel;
    private Label lblName;
    private Label lblCamp;
    private Label lblSkill;
    private Label lblSkillDesc;
    private Label lblPower;
    private Label lblWisdom;
    private StateMachine stateMachine;
    private AnimationPlayer animationPlayer;
    private ColorRect colorRect;

    private StyleBox clicked_style = ResourceLoader.Load<StyleBox>("res://resources/card_data/card_clicked_stylebox.tres");
    private string current_style = "base"; // 当前样式
    private bool showBack = false; // 是否显示背面 

    public override void _Ready()
    {
        // 引用对应节点
        soundManager = GetNode<SoundManager>("/root/SoundManager");
        visual = GetNode<PanelContainer>("可视化");
        panel = GetNode<Panel>("%面板");
        backPanel = GetNode<Panel>("%背面");
        lblName = GetNode<Label>("%姓名");
        lblCamp = GetNode<Label>("%阵容");
        lblSkill = GetNode<Label>("%技能名称");
        lblSkillDesc = GetNode<Label>("%技能描述");
        lblPower = GetNode<Label>("%武力");
        lblWisdom = GetNode<Label>("%智力");
        stateMachine = GetNode<StateMachine>("有限状态机");
        animationPlayer = GetNode<AnimationPlayer>("动画");
        colorRect = GetNode<ColorRect>("%幕布");

        // 启动状态机
        stateMachine.LaunchStateMachine();

        // 初始化卡牌基本参数
        Init();

        // 订阅输入事件
        GuiInput += Card_GuiInput;
        MouseEntered += Card_MouseEntered;
        MouseExited += Card_MouseExited;
    }

    public override void _Input(InputEvent @event)
    {
        stateMachine.OnInput(@event);
    }

    private void Init()
    {
        if (cardData == null) return;
        // 根据阵容设置卡牌不同背景
        switch (cardData.Camp)
        {
            case CardData.CampType.Shu:
                StyleBoxTexture styleShu = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_shu.tres");
                panel.Set("theme_override_styles/panel", styleShu);
                lblCamp.Text = "蜀";
                break;
            case CardData.CampType.Wei:
                StyleBoxTexture styleWei = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_wei.tres");
                panel.Set("theme_override_styles/panel", styleWei);
                lblCamp.Text = "魏";
                break;
            case CardData.CampType.Wu:
                StyleBoxTexture styleWu = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_wu.tres");
                panel.Set("theme_override_styles/panel", styleWu);
                lblCamp.Text = "吴";
                break;
            default: break;
        }
        // 设置卡牌属性
        lblName.Text = cardData.Name;
        lblSkill.Text = cardData.Skill;
        lblSkillDesc.Text = cardData.SkillDesc;
        lblPower.Text = "武：" + cardData.Power.ToString();
        lblWisdom.Text = "智：" + cardData.Wisdom.ToString();
        // 是否显示背面
        backPanel.Visible = showBack;
        // 是否被遮挡
        colorRect.Visible = false;
        // 初始化增量
        cardData.AdditionalPower = 0;
        cardData.AdditionalWisdom = 0;
    }

    private void Card_MouseExited()
    {
        stateMachine.OnMouseExit();
    }

    private void Card_MouseEntered()
    {
        stateMachine.OnMouseEnter();
    }

    private void Card_GuiInput(InputEvent @event)
    {
        stateMachine.OnGuiInput(@event);
    }

    /// <summary>
    /// 改变卡牌状态
    /// </summary>
    /// <param name="state"></param>
    public void ChangedToState(BaseState.State state)
    {
        if (stateMachine.GetCurrentState() == state) return;
        switch (state)
        {
            case BaseState.State.Normal:
                // 如果是点击状态，需要先释放
                if (stateMachine.GetCurrentState()== BaseState.State.Clicked)
                {
                    GetNode<GameEvent>("/root/GameEvent").EmitSignal(GameEvent.SignalName.CardReleased, this);
                }
                stateMachine.ChangeToState<CardNormalState>();
                // 如果上一个动画为选择就播放取消动画
                if (animationPlayer.AssignedAnimation == "选择")
                {
                    animationPlayer.Play("取消");
                }
                break;
            case BaseState.State.Clicked:
                stateMachine.ChangeToState<CardClickedState>();
                break;
            case BaseState.State.Played:
                stateMachine.ChangeToState<CardPlayedState>();
                // 这里直接让卡牌回到初始动画
                animationPlayer.Play("RESET");
                break;
            case BaseState.State.Discard:
                stateMachine.ChangeToState<CardDiscardState>();
                break;
            default: break;
        }
    }

    /// <summary>
    /// 播放指定动画
    /// </summary>
    /// <param name="ani"></param>
    public void PlayAni(string ani)
    {
        animationPlayer.Play(ani);
        if (ani == "选择")
        {
            soundManager.PlaySFX("切换");
        }
        else if (ani == "攻击")
        {
            soundManager.PlaySFX("攻击");
        }
    }

    /// <summary>
    /// 等待动画播放结束
    /// </summary>
    /// <param name="ani"></param>
    public async Task PlayAniAsync(string ani)
    {
        animationPlayer.Play(ani);
        if (ani == "选择")
        {
            soundManager.PlaySFX("切换");
        }
        else if (ani == "攻击")
        {
            soundManager.PlaySFX("攻击");
        }
        await ToSignal(animationPlayer, "animation_finished");
    }

    /// <summary>
    /// 改变卡牌样式
    /// </summary>
    /// <param name="styleName"></param>
    public void ChangedStyleBox(string styleName)
    {
        if (current_style == styleName) return;
        current_style = styleName;
        if (styleName == "clicked")
        {
            visual.Set("theme_override_styles/panel", clicked_style);
        }
        else
        {
            visual.RemoveThemeStyleboxOverride("panel");
        }
    }

    /// <summary>
    /// 设置背面显示
    /// </summary>
    /// <param name="isShow"></param>
    public void SetBackShow(bool isShow)
    {
        showBack = isShow;
        backPanel.Visible = showBack;
    }

    /// <summary>
    /// 设置是否显示遮挡
    /// </summary>
    /// <param name="isOcc"></param>
    public void SetOcclusion(bool isOcc)
    {
        colorRect.Visible = isOcc;
    }
}
