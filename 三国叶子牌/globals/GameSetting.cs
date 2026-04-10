using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 游戏音频设置
/// </summary>
public class GameAudioSetting
{
    /*
     * 还可以考虑设置BGM列表、输出设备等等
     */

    /// <summary>
    /// 总音量
    /// </summary>
    public float masterSize {  get; set; }
    /// <summary>
    /// 背景
    /// </summary>
    public float bgmSize { get; set; }
    /// <summary>
    /// 音效
    /// </summary>
    public float sfxSize{ get; set; }
}
