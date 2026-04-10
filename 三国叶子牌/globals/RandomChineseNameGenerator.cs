using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 随机中文名字生成器
/// </summary>
public static class RandomChineseNameGenerator
{
    // 静态随机数生成器，带线程锁以保证线程安全
    private static readonly Random _random = new Random();
    private static readonly object _lock = new object();

    // 常见单姓
    private static readonly string[] _singleFamilyNames = new[]
    {
        "李", "王", "张", "刘", "陈", "杨", "赵", "黄", "周", "吴",
        "徐", "孙", "胡", "朱", "高", "林", "何", "郭", "马", "罗",
        "梁", "宋", "郑", "谢", "韩", "唐", "冯", "于", "董", "萧",
        "程", "曹", "袁", "邓", "许", "傅", "沈", "曾", "彭", "吕"
    };

    // 常见复姓
    private static readonly string[] _compoundFamilyNames = new[]
    {
        "欧阳", "太史", "端木", "上官", "司马", "东方", "独孤", "南宫",
        "夏侯", "诸葛", "尉迟", "公羊", "赫连", "澹台", "皇甫", "宗政",
        "濮阳", "淳于", "单于", "申屠", "公孙", "仲孙", "轩辕", "令狐",
        "钟离", "宇文", "长孙", "慕容", "鲜于", "闾丘", "司徒", "司空",
        "亓官", "司寇", "子车", "颛孙", "端木", "巫马", "公西", "漆雕"
    };

    // 男性名字常用字（单字）
    private static readonly string[] _maleGivenNames = new[]
    {
        "伟", "强", "磊", "洋", "勇", "军", "杰", "涛", "斌", "超",
        "明", "刚", "健", "俊", "帅", "宇", "鹏", "飞", "龙", "虎",
        "林", "波", "辉", "晨", "浩", "然", "宇", "嘉", "铭", "泽"
    };

    // 女性名字常用字（单字）
    private static readonly string[] _femaleGivenNames = new[]
    {
        "芳", "娜", "娟", "静", "敏", "燕", "艳", "丽", "颖", "琳",
        "婷", "娇", "娅", "妮", "倩", "雪", "云", "霞", "雯", "莹",
        "淑", "慧", "巧", "雅", "艺", "慧", "丹", "佳", "梦", "媛"
    };

    // 中性名字常用字（单字，适合男女）
    private static readonly string[] _neutralGivenNames = new[]
    {
        "天", "一", "子", "文", "思", "晓", "海", "小", "安", "宁",
        "欣", "乐", "悦", "舒", "智", "博", "瑞", "琳", "琪", "阳"
    };

    // 用于组合双名（第二个字）的常用字，通常更柔和或通用
    private static readonly string[] _secondaryGivenNames = new[]
    {
        "杰", "伟", "涛", "勇", "军", "峰", "俊", "鹏", "宇", "浩",
        "然", "铭", "泽", "洋", "波", "辉", "晨", "曦", "轩", "瑞",
        "琪", "琳", "颖", "婷", "娜", "媛", "雪", "雯", "倩", "丹"
    };

    /// <summary>
    /// 生成一个随机中文名字
    /// </summary>
    /// <param name="gender">性别倾向：0 随机，1 男性倾向，2 女性倾向（默认随机）</param>
    /// <param name="allowCompoundFamily">是否允许使用复姓（默认 true）</param>
    /// <param name="nameLength">名字字数：1 单字，2 双字，0 随机（默认随机）</param>
    /// <returns>中文姓名</returns>
    public static string Generate(int gender = 0, bool allowCompoundFamily = true, int nameLength = 0)
    {
        lock (_lock)
        {
            // 1. 选择姓氏
            string familyName;
            if (allowCompoundFamily && _random.NextDouble() < 0.2) // 20% 概率使用复姓
            {
                familyName = _compoundFamilyNames[_random.Next(_compoundFamilyNames.Length)];
            }
            else
            {
                familyName = _singleFamilyNames[_random.Next(_singleFamilyNames.Length)];
            }

            // 2. 确定名字长度
            int givenNameLength = nameLength;
            if (givenNameLength == 0)
            {
                givenNameLength = _random.Next(1, 3); // 随机 1 或 2
            }

            // 3. 根据性别倾向选择名字字库
            string[] primaryPool;
            switch (gender)
            {
                case 1: // 男性倾向
                    primaryPool = _maleGivenNames;
                    break;
                case 2: // 女性倾向
                    primaryPool = _femaleGivenNames;
                    break;
                default: // 随机
                    primaryPool = _neutralGivenNames;
                    break;
            }

            // 4. 生成名字
            string givenName;
            if (givenNameLength == 1)
            {
                givenName = primaryPool[_random.Next(primaryPool.Length)];
            }
            else // 双字名
            {
                // 第一个字从性别倾向字库取，第二个字从通用字库取
                string first = primaryPool[_random.Next(primaryPool.Length)];
                string second = _secondaryGivenNames[_random.Next(_secondaryGivenNames.Length)];
                givenName = first + second;
            }

            return familyName + givenName;
        }
    }

    /// <summary>
    /// 生成一个随机的男性倾向中文名字
    /// </summary>
    public static string GenerateMale(bool allowCompoundFamily = true, int nameLength = 0)
    {
        return Generate(1, allowCompoundFamily, nameLength);
    }

    /// <summary>
    /// 生成一个随机的女性倾向中文名字
    /// </summary>
    public static string GenerateFemale(bool allowCompoundFamily = true, int nameLength = 0)
    {
        return Generate(2, allowCompoundFamily, nameLength);
    }

    /// <summary>
    /// 生成一个8位的随机数
    /// </summary>
    /// <returns></returns>
    public static string GenerateSimple()
    {
        // 基于时间戳的低位截断，约 11.5 天一个循环
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); // 13 位
        long low8 = timestamp % 100_000_000; // 取低 8 位
        return low8.ToString();
    }
}
