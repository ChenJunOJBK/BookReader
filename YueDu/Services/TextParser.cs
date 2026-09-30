using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YueDu.Models;

namespace YueDu.Services;

/// <summary>
/// TXT 文本解析服务
/// </summary>
public static class TextParser
{
    /// <summary>
    /// 章节标题匹配正则（支持中文第X章/回/节/卷，英文 Chapter X，以及 数字.标题 等常见格式）
    /// </summary>
    private static readonly Regex[] ChapterPatterns = new[]
    {
        // 第X章 / 第X回 / 第X节 / 第X卷 / 第X集 / 第X篇
        new Regex(@"^\s*第\s*[\d一二三四五六七八九十百千零〇两]+?\s*[章回节卷集篇幕部]", RegexOptions.Compiled),
        // Chapter 1 / Chapter One
        new Regex(@"^\s*Chapter\s+\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        // 卷一、卷二等
        new Regex(@"^\s*[卷][\d一二三四五六七八九十百千零〇两]+?\s", RegexOptions.Compiled),
        // 1. 标题 / 1、标题
        new Regex(@"^\s*\d{1,4}\s*[.、．]\s*\S", RegexOptions.Compiled),
    };

    /// <summary>
    /// 读取TXT文件并自动识别编码
    /// </summary>
    public static string ReadTextFile(string filePath)
    {
        // 先尝试UTF-8 BOM，再尝试GBK，最后UTF-8无BOM
        var bytes = File.ReadAllBytes(filePath);

        // UTF-8 BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

        // 尝试 UTF-8 解码，若无乱码则使用
        var utf8 = Encoding.UTF8.GetString(bytes);
        if (!ContainsReplacementChar(utf8))
            return utf8;

        // GBK / GB2312
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var gbk = Encoding.GetEncoding("GBK");
            return gbk.GetString(bytes);
        }
        catch
        {
            return utf8;
        }
    }

    private static bool ContainsReplacementChar(string text)
    {
        // U+FFFD 替换字符表示解码失败
        return text.Contains('\uFFFD');
    }

    /// <summary>
    /// 将文本按章节分割
    /// </summary>
    public static List<Chapter> SplitIntoChapters(string text)
    {
        var chapters = new List<Chapter>();
        if (string.IsNullOrWhiteSpace(text))
            return chapters;

        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var currentTitle = "正文";
        var content = new StringBuilder();
        long charIndex = 0;

        void Flush()
        {
            var chapterText = content.ToString().Trim();
            if (!string.IsNullOrEmpty(chapterText) || chapters.Count == 0)
            {
                chapters.Add(new Chapter
                {
                    Title = currentTitle,
                    Content = chapterText,
                    StartCharIndex = charIndex
                });
                charIndex += chapterText.Length;
            }
            content.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            if (IsChapterTitle(line))
            {
                Flush();
                currentTitle = line.Trim();
            }
            else
            {
                if (content.Length > 0) content.Append('\n');
                content.Append(line);
            }
        }
        Flush();

        // 如果只有一个"正文"章节，说明没有识别到章节，尝试按固定长度分块
        if (chapters.Count <= 1 && chapters.Count > 0 && chapters[0].Content.Length > 5000)
        {
            return SplitByLength(chapters[0].Content, 3000);
        }

        return chapters;
    }

    /// <summary>
    /// 判断一行是否是章节标题
    /// </summary>
    private static bool IsChapterTitle(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return false;
        var trimmed = line.Trim();
        // 章节标题通常较短
        if (trimmed.Length > 60) return false;

        foreach (var pattern in ChapterPatterns)
        {
            if (pattern.IsMatch(trimmed))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 按固定长度分块（无章节标题时的兜底）
    /// </summary>
    private static List<Chapter> SplitByLength(string text, int chunkSize)
    {
        var chapters = new List<Chapter>();
        long index = 0;
        int i = 0;
        while (index < text.Length)
        {
            int len = Math.Min(chunkSize, text.Length - (int)index);
            // 尽量在换行处分割
            if (index + len < text.Length)
            {
                int nl = text.LastIndexOf('\n', (int)index + len, chunkSize / 2);
                if (nl > index) len = nl - (int)index + 1;
            }
            chapters.Add(new Chapter
            {
                Title = $"第 {++i} 段",
                Content = text.Substring((int)index, len).Trim(),
                StartCharIndex = index
            });
            index += len;
        }
        return chapters;
    }

    /// <summary>
    /// 从文件名中提取书名（去掉扩展名）
    /// </summary>
    public static string GetTitleFromFileName(string filePath)
    {
        return Path.GetFileNameWithoutExtension(filePath);
    }
}
