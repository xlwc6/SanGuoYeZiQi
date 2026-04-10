using Godot;
using System;
using System.Text;

public partial class CardData : Resource
{
    /// <summary>
    /// 阵营枚举
    /// </summary>
    public enum CampType
    {
        Qun, // 群
        Shu, // 蜀
        Wei, // 魏
        Wu,  // 吴
    }
    /// <summary>
    /// 技能类型枚举
    /// </summary>
    public enum SkillType
    {
        Before, // 战斗之前
        Normal, // 战斗时
        After,  // 战斗之后
    }
    /// <summary>
    /// 姓名
    /// </summary>
    [Export]
    public string Name { get; set; }
    /// <summary>
    /// 阵营
    /// </summary>
    [Export]
    public CampType Camp { get; set; }
    /// <summary>
    /// 基础武力
    /// </summary>
    [Export]
    public int Power { get; set; }
    /// <summary>
    /// 基础智力
    /// </summary>
    [Export]
    public int Wisdom { get; set; }
    /// <summary>
    /// 技能名称
    /// </summary>
    [Export]
    public string Skill { get; set; }
    /// <summary>
    /// 技能描述
    /// </summary>
    [Export(PropertyHint.MultilineText)]
    public string SkillDesc { get; set; }
    /// <summary>
    /// 技能类型
    /// </summary>
    [Export]
    public SkillType Type { get; set; } = SkillType.Normal;
    /// <summary>
    /// 武力增量
    /// </summary>
    public int AdditionalPower { get; set; }
    /// <summary>
    /// 智力增量
    /// </summary>
    public int AdditionalWisdom { get; set; }

    public new string ToString()
    {
        StringBuilder result = new();
        result.Append($"武将：{Name} - {Camp.ToString()}；");
        result.Append($"武力：{Power} {AdditionalPower}，智力：{Wisdom} {AdditionalWisdom}；");
        result.Append($"技能：{Skill}，{SkillDesc}");
        return result.ToString();
    }

    /// <summary>
    /// 比较卡牌
    /// </summary>
    /// <param name="targetCard"></param>
    /// <returns>分数：1 比对方大， 0 平局， -1 比对方小</returns>
    public int Compare(CardData targetCard)
    {
        // 战前技能判断
        if (targetCard.Type == SkillType.Before || this.Type == SkillType.Before)
        {
            var specJud = SpecialSkillJudgment(this, targetCard);
            if (specJud != null)
            {
                return (int)specJud;
            }
            var befoeJud = BeforeSkillJudgment(this, targetCard);
            if (befoeJud != null)
            {
                return (int)befoeJud;
            }
        }
        // 战斗中技能判断
        if (targetCard.Type == SkillType.Normal || this.Type == SkillType.Normal)
        {
            var normalJud = NormalSkillJudgment(this, targetCard);
            if (normalJud != null)
            {
                return (int)normalJud;
            }
        }
        // 直接判断武力，战后技能不在这里判断
        int minus = Mathf.Clamp(this.Power + this.AdditionalPower - targetCard.Power - targetCard.AdditionalPower, -1, 1);
        return minus;
    }

    #region 技能比较实现

    // 特殊技能判断，返回空，表示没有特殊技能或者不生效，否则返回自己可以获得分数
    private int? SpecialSkillJudgment(CardData self, CardData other)
    {
        // 是否有特殊战前技能，特殊技能很少且不冲突，就按智力顺序判断
        if (self.Skill == "红颜" && (other.Name == "吕布" || other.Name == "曹操" || other.Name == "关羽"))
        {
            return 1;
        }
        if (other.Skill == "红颜" && (self.Name == "吕布" || self.Name == "曹操" || self.Name == "关羽"))
        {
            return -1;
        }
        if (self.Skill == "神射" || other.Skill == "神射")
        {
            int minus = Mathf.Clamp(self.Power + self.AdditionalPower - other.Power - other.AdditionalPower, -1, 1);
            return minus;
        }
        if (self.Skill == "天子" && other.Camp != CardData.CampType.Wei)
        {
            return 1;
        }
        if (other.Skill == "天子" && self.Camp != CardData.CampType.Wei)
        {
            return -1;
        }
        return null;
    }

    // 先手技能判断
    private int? BeforeSkillJudgment(CardData self, CardData other)
    {
        // 确定谁先判断，小于0就对方先判断
        int minus = self.Wisdom - other.Wisdom;
        if (minus < 0)
        {
            if (other.Type == CardData.SkillType.Before)
            {
                int? ret = BeforeSkillBattle(other, self);
                return ret.HasValue ? -ret.Value : null;
            }
        }
        else
        {
            if (self.Type == CardData.SkillType.Before)
            {
                return BeforeSkillBattle(self, other);
            }
        }
        return null;
    }

    // 判断先手是否能赢过后手，并返回分数
    private int? BeforeSkillBattle(CardData first, CardData last)
    {
        if (first.Skill == "望族" && last.Camp == CardData.CampType.Qun) return 1;
        if (first.Skill == "昭烈" && last.Camp == CardData.CampType.Shu) return 1;
        if (first.Skill == "奸雄" && (last.Camp == CardData.CampType.Wei || last.Name == "吕布")) return 1;
        if (first.Skill == "鼎峙" && last.Camp == CardData.CampType.Wu) return 1;
        if (first.Skill == "结盟" && (last.Camp == CardData.CampType.Shu || last.Camp == CardData.CampType.Wu)) return 0;
        if (first.Skill == "虎痴" && (last.Name == "张飞" || last.Name == "马超")) return 0;
        return null;
    }

    // 战斗中技能判断
    private int? NormalSkillJudgment(CardData self, CardData other)
    {
        // 确定谁先判断，小于0就对方先判断
        int minus = self.Wisdom - other.Wisdom;
        if (minus < 0)
        {
            if (other.Type == CardData.SkillType.Normal && !string.IsNullOrEmpty(other.Skill))
            {
                int? ret = NormalSkillBattle(other, self);
                return ret.HasValue ? -ret.Value : null;
            }
        }
        else
        {
            if (self.Type == CardData.SkillType.Normal && !string.IsNullOrEmpty(self.Skill))
            {
                return NormalSkillBattle(self, other);
            }
        }
        return null;
    }

    // 判断战斗中是否能赢过后手，并返回分数
    private int? NormalSkillBattle(CardData first, CardData last)
    {
        // 判断先手是否能赢，由于技能是按智力高的先手，只有先手赢不了时才会减少后手的属性
        if (first.Skill == "铁骑" && last.Camp == CardData.CampType.Wei)
        {
            last.AdditionalPower -= 10;
        }
        if (first.Skill == "老将")
        {
            last.AdditionalWisdom -= 20;
        }
        if (first.Skill == "奇术" && last.Camp != CardData.CampType.Shu)
        {
            first.AdditionalPower += 15;
        }
        if (first.Skill == "克复" && last.Camp == CardData.CampType.Wei && (first.Wisdom > last.Wisdom))
        {
            return 1;
        }
        if (first.Skill == "代立" && last.Camp == CardData.CampType.Wei && last.Name != "曹操")
        {
            return 1;
        }
        if (first.Skill == "威震" && last.Camp == CardData.CampType.Wu && (first.Wisdom > last.Wisdom))
        {
            return 1;
        }
        if (first.Skill == "摧营" && last.Camp == CardData.CampType.Shu && (first.Wisdom > last.Wisdom))
        {
            return 1;
        }
        if (first.Skill == "劫营" && last.Camp == CardData.CampType.Wei && (first.Wisdom > last.Wisdom))
        {
            return 1;
        }
        // 诸葛亮技能特殊，最后判断
        if ((first.Skill == "神算" && last.Camp != CardData.CampType.Shu) || (last.Skill == "神算" && first.Camp != CardData.CampType.Shu))
        {
            int minus = Mathf.Clamp(first.Wisdom + first.AdditionalWisdom - last.Wisdom - last.AdditionalWisdom, -1, 1);
            return minus;
        }
        return null;
    }

    #endregion
}
