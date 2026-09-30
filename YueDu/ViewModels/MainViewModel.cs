using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using YueDu.Models;
using YueDu.Services;

namespace YueDu.ViewModels;

/// <summary>
/// 主视图模型：管理页面导航与共享服务
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    public BookService BookService { get; }
    public TtsService Tts { get; }

    private object? _currentView;
    private BookShelfViewModel? _shelfVm;
    private ReaderViewModel? _readerVm;

    public MainViewModel()
    {
        BookService = new BookService();
        BookService.Load();
        Tts = new TtsService();

        // 应用保存的TTS设置
        Tts.Rate = BookService.Settings.TtsRate;
        Tts.Volume = BookService.Settings.TtsVolume;
        if (!string.IsNullOrEmpty(BookService.Settings.TtsVoice))
            Tts.SelectVoice(BookService.Settings.TtsVoice);

        _shelfVm = new BookShelfViewModel(this);
        CurrentView = _shelfVm;
    }

    /// <summary>当前显示的页面视图模型</summary>
    public object? CurrentView
    {
        get => _currentView;
        set { _currentView = value; OnPropertyChanged(); }
    }

    /// <summary>打开阅读器</summary>
    public void OpenReader(Book book)
    {
        _readerVm = new ReaderViewModel(this, book);
        CurrentView = _readerVm;
    }

    /// <summary>返回书架</summary>
    public void BackToShelf()
    {
        // 关闭阅读器时停止朗读
        Tts.Stop();
        _readerVm = null;
        CurrentView = _shelfVm;
        _shelfVm?.Refresh();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
