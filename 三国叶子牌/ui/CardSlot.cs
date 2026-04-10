using Godot;
using System;

/// <summary>
/// 卡牌槽（占位展示）
/// </summary>
public partial class CardSlot : Panel
{
    /// <summary>
    /// 选择事件
    /// </summary>
    [Signal]
    public delegate void SelectedEventHandler(bool flag, CardData cardData);

    [Export]
    public CardData cardData;

    public bool IsSelected;

    private Label lblName;
    private Label lblCamp;
    private Label lblSkill;
    private Label lblSkillDesc;
    private Label lblPower;
    private Label lblWisdom;
    private TextureButton toggleBtn;
    private Line2D lineBorder;
    private ColorRect colorRect;

    public override void _Ready()
    {
        lblName = GetNode<Label>("%姓名");
        lblCamp = GetNode<Label>("%阵容");
        lblSkill = GetNode<Label>("%技能名称");
        lblSkillDesc = GetNode<Label>("%技能描述");
        lblPower = GetNode<Label>("%武力");
        lblWisdom = GetNode<Label>("%智力");
        toggleBtn = GetNode<TextureButton>("选择按钮");
        lineBorder = GetNode<Line2D>("轮廓");
        colorRect = GetNode<ColorRect>("幕布");

        toggleBtn.Toggled += ToggleBtn_Toggled;

        Init();
    }

    private void ToggleBtn_Toggled(bool toggledOn)
    {
        lineBorder.Visible = toggledOn;
        EmitSignal(SignalName.Selected, toggledOn, cardData);
    }

    private void Init()
    {
        if (cardData == null) return;
        // 根据阵容设置卡牌不同背景
        switch (cardData.Camp)
        {
            case CardData.CampType.Shu:
                StyleBoxTexture styleShu = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_shu.tres");
                Set("theme_override_styles/panel", styleShu);
                lblCamp.Text = "蜀";
                break;
            case CardData.CampType.Wei:
                StyleBoxTexture styleWei = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_wei.tres");
                Set("theme_override_styles/panel", styleWei);
                lblCamp.Text = "魏";
                break;
            case CardData.CampType.Wu:
                StyleBoxTexture styleWu = GD.Load<StyleBoxTexture>("res://resources/card_data/card_panel_wu.tres");
                Set("theme_override_styles/panel", styleWu);
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
        // 是否被遮挡
        colorRect.Visible = false;
        lineBorder.Visible = false;
    }

    /// <summary>
    /// 设置禁用
    /// </summary>
    /// <param name="flag"></param>
    public void SetDisabled(bool flag)
    {
        colorRect.Visible = flag;
        toggleBtn.Disabled = flag;
    }
}
