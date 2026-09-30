using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace YueDu.Models;

/// <summary>
/// 书籍信息
/// </summary>
public class Book : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _author = string.Empty;
    private string _filePath = string.Empty;
    private string _cover = string.Empty;
    private long _totalChars;
    private DateTime _addedAt;
    private DateTime _lastReadAt;

    /// <summary>书籍唯一ID</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>书名</summary>
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    /// <summary>作者</summary>
    public string Author
    {
        get => _author;
        set { _author = value; OnPropertyChanged(); }
    }

    /// <summary>源文件路径</summary>
    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }

    /// <summary>封面（可选）</summary>
    public string Cover
    {
        get => _cover;
        set { _cover = value; OnPropertyChanged(); }
    }

    /// <summary>总字数</summary>
    public long TotalChars
    {
        get => _totalChars;
        set { _totalChars = value; OnPropertyChanged(); }
    }

    /// <summary>导入时间</summary>
    public DateTime AddedAt
    {
        get => _addedAt;
        set { _addedAt = value; OnPropertyChanged(); }
    }

    /// <summary>最后阅读时间</summary>
    public DateTime LastReadAt
    {
        get => _lastReadAt;
        set { _lastReadAt = value; OnPropertyChanged(); }
    }

    /// <summary>章节列表</summary>
    public List<Chapter> Chapters { get; set; } = new();

    /// <summary>总章节数</summary>
    public int ChapterCount => Chapters.Count;

    /// <summary>当前阅读章节索引</summary>
    public int CurrentChapterIndex { get; set; }

    /// <summary>当前章节内字符偏移</summary>
    public int CurrentCharOffset { get; set; }

    /// <summary>已读字数</summary>
    public long ReadChars
    {
        get
        {
            long chars = 0;
            for (int i = 0; i < CurrentChapterIndex && i < Chapters.Count; i++)
                chars += Chapters[i].Content.Length;
            if (CurrentChapterIndex < Chapters.Count)
                chars += Math.Min(CurrentCharOffset, Chapters[CurrentChapterIndex].Content.Length);
            return chars;
        }
    }

    /// <summary>阅读进度百分比</summary>
    public double ProgressPercent => TotalChars > 0 ? (double)ReadChars / TotalChars * 100 : 0;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// 章节
/// </summary>
public class Chapter
{
    /// <summary>章节标题</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>章节内容</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>章节在全文中的起始字符位置</summary>
    public long StartCharIndex { get; set; }
}
