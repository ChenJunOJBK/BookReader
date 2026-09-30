using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Media;
using YueDu.Models;
using YueDu.Services;

namespace YueDu.ViewModels;

/// <summary>
/// 阅读器视图模型
/// </summary>
public class ReaderViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;
    private readonly Book _book;

    private string _chapterTitle = string.Empty;
    private string _chapterContent = string.Empty;
    private int _chapterIndex;
    private double _fontSize;
    private string _fontFamily = string.Empty;
    private string _theme = "sepia";
    private double _lineSpacing;
    private bool _isSpeaking;
    private bool _isPaused;
    private bool _continuousRead;
    private bool _isChapterPanelOpen;
    private int _ttsRate;
    private int _ttsVolume;
    private string _selectedVoice = string.Empty;

    /// <summary>自动滚动请求（参数为 0~1 的进度比例）</summary>
    public event Action<double>? AutoScrollRequested;

    public ReaderViewModel(MainViewModel main, Book book)
    {
        _main = main;
        _book = book;

        var s = _main.BookService.Settings;
        _fontSize = s.FontSize;
        _fontFamily = s.FontFamily;
        _theme = s.Theme;
        _lineSpacing = s.LineSpacing;
        _ttsRate = s.TtsRate;
        _ttsVolume = s.TtsVolume;
        _selectedVoice = s.TtsVoice;

        _chapterIndex = Math.Clamp(book.CurrentChapterIndex, 0, Math.Max(0, book.Chapters.Count - 1));
        LoadChapter(_chapterIndex);

        // 初始化语音列表
        Voices = new ObservableCollection<InstalledVoice>(_main.Tts.GetInstalledVoices());
        if (Voices.Count > 0 && string.IsNullOrEmpty(_selectedVoice))
            _selectedVoice = Voices[0].VoiceInfo.Name;

        // 监听TTS状态
        _main.Tts.SpeakStarted += () => IsSpeaking = true;
        _main.Tts.SpeakCompleted += OnSpeakCompleted;
        _main.Tts.PositionChanged += OnPositionChanged;
    }

    /// <summary>当前书籍</summary>
    public Book Book => _book;

    /// <summary>章节列表</summary>
    public List<Chapter> ChapterList => _book.Chapters;

    /// <summary>章节目录面板是否展开</summary>
    public bool IsChapterPanelOpen
    {
        get => _isChapterPanelOpen;
        set { _isChapterPanelOpen = value; OnPropertyChanged(); }
    }

    /// <summary>章节标题</summary>
    public string ChapterTitle
    {
        get => _chapterTitle;
        set { _chapterTitle = value; OnPropertyChanged(); }
    }

    /// <summary>章节正文</summary>
    public string ChapterContent
    {
        get => _chapterContent;
        set { _chapterContent = value; OnPropertyChanged(); }
    }

    /// <summary>当前章节索引（0-based）</summary>
    public int ChapterIndex
    {
        get => _chapterIndex;
        private set { _chapterIndex = value; OnPropertyChanged(); OnPropertyChanged(nameof(ChapterDisplay)); }
    }

    /// <summary>章节显示文本 "第 x / y 章"</summary>
    public string ChapterDisplay => $"{ChapterIndex + 1} / {_book.Chapters.Count}";

    /// <summary>字号</summary>
    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineHeightPixels)); SaveSettings(); }
    }

    /// <summary>字体</summary>
    public string FontFamily
    {
        get => _fontFamily;
        set { _fontFamily = value; OnPropertyChanged(); SaveSettings(); }
    }

    /// <summary>主题</summary>
    public string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLightTheme));
            OnPropertyChanged(nameof(IsSepiaTheme));
            OnPropertyChanged(nameof(IsDarkTheme));
            OnPropertyChanged(nameof(BackgroundBrush));
            OnPropertyChanged(nameof(ForegroundBrush));
            SaveSettings();
        }
    }

    public bool IsLightTheme => Theme == "light";
    public bool IsSepiaTheme => Theme == "sepia";
    public bool IsDarkTheme => Theme == "dark";

    /// <summary>背景画刷</summary>
    public Brush BackgroundBrush
    {
        get
        {
            return Theme switch
            {
                "light" => new SolidColorBrush(Colors.White),
                "dark" => new SolidColorBrush(Color.FromRgb(30, 30, 46)),
                _ => new SolidColorBrush(Color.FromRgb(245, 236, 215))
            };
        }
    }

    /// <summary>前景画刷</summary>
    public Brush ForegroundBrush
    {
        get
        {
            return Theme switch
            {
                "light" => new SolidColorBrush(Color.FromRgb(34, 34, 34)),
                "dark" => new SolidColorBrush(Color.FromRgb(212, 212, 216)),
                _ => new SolidColorBrush(Color.FromRgb(90, 70, 50))
            };
        }
    }

    /// <summary>行间距倍数</summary>
    public double LineSpacing
    {
        get => _lineSpacing;
        set { _lineSpacing = value; OnPropertyChanged(); OnPropertyChanged(nameof(LineHeightPixels)); SaveSettings(); }
    }

    /// <summary>实际行高（像素）= 字号 * 倍数</summary>
    public double LineHeightPixels => FontSize * LineSpacing;

    /// <summary>可用语音列表</summary>
    public ObservableCollection<InstalledVoice> Voices { get; }

    /// <summary>选中的语音</summary>
    public string SelectedVoice
    {
        get => _selectedVoice;
        set
        {
            _selectedVoice = value;
            OnPropertyChanged();
            _main.Tts.SelectVoice(value);
            SaveSettings();
        }
    }

    /// <summary>是否正在朗读</summary>
    public bool IsSpeaking
    {
        get => _isSpeaking;
        set { _isSpeaking = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeakButtonText)); }
    }

    /// <summary>是否已暂停</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeakButtonText)); }
    }

    /// <summary>朗读按钮文字</summary>
    public string SpeakButtonText => IsSpeaking ? (IsPaused ? "继续" : "暂停") : "朗读";

    /// <summary>语速</summary>
    public int TtsRate
    {
        get => _ttsRate;
        set { _ttsRate = value; OnPropertyChanged(); _main.Tts.Rate = value; SaveSettings(); }
    }

    /// <summary>音量</summary>
    public int TtsVolume
    {
        get => _ttsVolume;
        set { _ttsVolume = value; OnPropertyChanged(); _main.Tts.Volume = value; SaveSettings(); }
    }

    /// <summary>朗读完成回调：自动下一章或结束</summary>
    private void OnSpeakCompleted()
    {
        if (_continuousRead && _chapterIndex < _book.Chapters.Count - 1)
        {
            // 自动翻到下一章并继续朗读
            Application.Current.Dispatcher.Invoke(() =>
            {
                LoadChapter(_chapterIndex + 1);
                _main.Tts.SelectVoice(_selectedVoice);
                _main.Tts.Rate = _ttsRate;
                _main.Tts.Volume = _ttsVolume;
                _main.Tts.Speak(ChapterContent);
            });
        }
        else
        {
            _continuousRead = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsSpeaking = false;
                IsPaused = false;
            });
        }
    }

    /// <summary>朗读进度回调：触发自动滚动</summary>
    private void OnPositionChanged(int charPosition)
    {
        if (!string.IsNullOrEmpty(ChapterContent) && charPosition > 0)
        {
            double progress = (double)charPosition / ChapterContent.Length;
            AutoScrollRequested?.Invoke(progress);
        }
    }

    /// <summary>加载指定章节</summary>
    private void LoadChapter(int index)
    {
        if (index < 0 || index >= _book.Chapters.Count) return;
        var ch = _book.Chapters[index];
        ChapterTitle = ch.Title;
        ChapterContent = ch.Content;
        ChapterIndex = index;
        _main.BookService.UpdateProgress(_book, index, 0);
    }

    /// <summary>上一章</summary>
    public void PrevChapter()
    {
        if (_chapterIndex > 0)
        {
            StopSpeaking();
            LoadChapter(_chapterIndex - 1);
        }
    }

    /// <summary>下一章</summary>
    public void NextChapter()
    {
        if (_chapterIndex < _book.Chapters.Count - 1)
        {
            StopSpeaking();
            LoadChapter(_chapterIndex + 1);
        }
    }

    /// <summary>切换章节目录面板</summary>
    public void ToggleChapterPanel()
    {
        IsChapterPanelOpen = !IsChapterPanelOpen;
    }

    /// <summary>跳转到指定章节</summary>
    public void JumpToChapter(int index)
    {
        if (index < 0 || index >= _book.Chapters.Count) return;
        StopSpeaking();
        IsChapterPanelOpen = false;
        LoadChapter(index);
    }

    /// <summary>返回书架</summary>
    public void BackToShelf()
    {
        StopSpeaking();
        SaveSettings();
        _main.BackToShelf();
    }

    /// <summary>切换朗读/暂停</summary>
    /// <param name="startOffset">开始朗读的字符偏移（从屏幕可见位置计算）</param>
    public void ToggleSpeak(int startOffset = 0)
    {
        if (IsSpeaking)
        {
            if (IsPaused)
            {
                _main.Tts.Resume();
                IsPaused = false;
            }
            else
            {
                _main.Tts.Pause();
                IsPaused = true;
            }
        }
        else
        {
            _continuousRead = true;
            _main.Tts.SelectVoice(_selectedVoice);
            _main.Tts.Rate = _ttsRate;
            _main.Tts.Volume = _ttsVolume;
            _main.Tts.Speak(ChapterContent, startOffset);
            IsSpeaking = true;
            IsPaused = false;
        }
    }

    /// <summary>停止朗读</summary>
    public void StopSpeaking()
    {
        _continuousRead = false;
        _main.Tts.Stop();
        IsSpeaking = false;
        IsPaused = false;
    }

    /// <summary>字号增大</summary>
    public void IncreaseFontSize() => FontSize = Math.Min(FontSize + 2, 48);

    /// <summary>字号减小</summary>
    public void DecreaseFontSize() => FontSize = Math.Max(FontSize - 2, 12);

    private void SaveSettings()
    {
        var s = _main.BookService.Settings;
        s.FontSize = _fontSize;
        s.FontFamily = _fontFamily;
        s.Theme = _theme;
        s.LineSpacing = _lineSpacing;
        s.TtsRate = _ttsRate;
        s.TtsVolume = _ttsVolume;
        s.TtsVoice = _selectedVoice;
        _main.BookService.SaveSettings();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
