using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using YueDu.Models;

namespace YueDu.Services;

/// <summary>
/// 书架与书籍管理服务
/// </summary>
public class BookService
{
    private readonly string _dataDir;
    private readonly string _shelfFile;
    private BookShelfData _data = new();

    public BookService()
    {
        _dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "YueDu");
        Directory.CreateDirectory(_dataDir);
        _shelfFile = Path.Combine(_dataDir, "bookshelf.json");
    }

    /// <summary>当前书架数据</summary>
    public BookShelfData Data => _data;

    /// <summary>书籍列表</summary>
    public List<Book> Books => _data.Books;

    /// <summary>应用设置</summary>
    public AppSettings Settings => _data.Settings;

    /// <summary>加载书架数据</summary>
    public void Load()
    {
        try
        {
            if (File.Exists(_shelfFile))
            {
                var json = File.ReadAllText(_shelfFile);
                _data = JsonSerializer.Deserialize<BookShelfData>(json) ?? new BookShelfData();
            }
        }
        catch
        {
            _data = new BookShelfData();
        }
    }

    /// <summary>保存书架数据</summary>
    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_shelfFile, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>导入TXT文件</summary>
    public Book? ImportBook(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var text = TextParser.ReadTextFile(filePath);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var chapters = TextParser.SplitIntoChapters(text);
        long totalChars = chapters.Sum(c => c.Content.Length);

        var book = new Book
        {
            Title = TextParser.GetTitleFromFileName(filePath),
            Author = "未知",
            FilePath = filePath,
            Chapters = chapters,
            TotalChars = totalChars,
            AddedAt = DateTime.Now,
            LastReadAt = DateTime.Now,
            CurrentChapterIndex = 0,
            CurrentCharOffset = 0
        };

        // 避免重复导入
        var existing = _data.Books.FirstOrDefault(b => b.FilePath == filePath);
        if (existing != null)
        {
            _data.Books.Remove(existing);
        }

        _data.Books.Insert(0, book);
        Save();
        return book;
    }

    /// <summary>删除书籍</summary>
    public void RemoveBook(Book book)
    {
        _data.Books.Remove(book);
        Save();
    }

    /// <summary>更新阅读进度</summary>
    public void UpdateProgress(Book book, int chapterIndex, int charOffset)
    {
        book.CurrentChapterIndex = chapterIndex;
        book.CurrentCharOffset = charOffset;
        book.LastReadAt = DateTime.Now;
        Save();
    }

    /// <summary>保存设置</summary>
    public void SaveSettings() => Save();
}
