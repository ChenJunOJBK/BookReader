using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YueDu.Models;

/// <summary>
/// 书架数据（用于序列化保存）
/// </summary>
public class BookShelfData
{
    /// <summary>书籍列表</summary>
    public List<Book> Books { get; set; } = new();

    /// <summary>应用设置</summary>
    public AppSettings Settings { get; set; } = new();
}

/// <summary>
/// 应用设置
/// </summary>
public class AppSettings
{
    /// <summary>字号</summary>
    public double FontSize { get; set; } = 20;

    /// <summary>字体</summary>
    public string FontFamily { get; set; } = "Microsoft YaHei";

    /// <summary>主题：light/dark/sepia</summary>
    public string Theme { get; set; } = "sepia";

    /// <summary>听书语速 (-10 ~ 10)</summary>
    public int TtsRate { get; set; } = 0;

    /// <summary>听书音量 0 ~ 100</summary>
    public int TtsVolume { get; set; } = 100;

    /// <summary>语音名称</summary>
    public string TtsVoice { get; set; } = string.Empty;

    /// <summary>行间距倍数</summary>
    public double LineSpacing { get; set; } = 1.5;
}
