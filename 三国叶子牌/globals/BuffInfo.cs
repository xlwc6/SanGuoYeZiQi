using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Buff信息类
/// </summary>
public class BuffInfo
{
    /// <summary>
    /// 作用对象
    /// </summary>
    public OwnerType Target { get; set; }
    /// <summary>
    /// 武力增量
    /// </summary>
    public int AdditionalPower { get; set; }
    /// <summary>
    /// 智力增量
    /// </summary>
    public int AdditionalWisdom { get; set; }
}
