using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YueDu.ViewModels;

namespace YueDu.Views;

public partial class ReaderView : UserControl
{
    private ReaderViewModel? _vm;

    public ReaderView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Unloaded += OnUnloaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        // 旧VM取消订阅
        if (_vm != null)
        {
            _vm.AutoScrollRequested -= OnAutoScroll;
        }

        _vm = e.NewValue as ReaderViewModel;

        // 新VM订阅
        if (_vm != null)
        {
            _vm.AutoScrollRequested += OnAutoScroll;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.AutoScrollRequested -= OnAutoScroll;
            _vm = null;
        }
    }

    /// <summary>自动滚动阅读区到朗读进度位置</summary>
    private void OnAutoScroll(double progress)
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (ReadingScrollViewer != null && ReadingScrollViewer.ScrollableHeight > 0)
            {
                double targetOffset = progress * ReadingScrollViewer.ScrollableHeight;
                ReadingScrollViewer.ScrollToVerticalOffset(targetOffset);
            }
        });
    }

    private void Back_Click(object sender, RoutedEventArgs e) => _vm?.BackToShelf();
    private void Prev_Click(object sender, RoutedEventArgs e) => _vm?.PrevChapter();
    private void Next_Click(object sender, RoutedEventArgs e) => _vm?.NextChapter();
    private void FontDec_Click(object sender, RoutedEventArgs e) => _vm?.DecreaseFontSize();
    private void FontInc_Click(object sender, RoutedEventArgs e) => _vm?.IncreaseFontSize();
    private void ThemeLight_Click(object sender, RoutedEventArgs e) { if (_vm != null) _vm.Theme = "light"; }
    private void ThemeSepia_Click(object sender, RoutedEventArgs e) { if (_vm != null) _vm.Theme = "sepia"; }
    private void ThemeDark_Click(object sender, RoutedEventArgs e) { if (_vm != null) _vm.Theme = "dark"; }
    private void Speak_Click(object sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        // 开始朗读时，从屏幕可见的第一行开始
        int startOffset = GetVisibleCharOffset();
        _vm.ToggleSpeak(startOffset);
    }

    private void Stop_Click(object sender, RoutedEventArgs e) => _vm?.StopSpeaking();

    private void ChapterPanel_Click(object sender, RoutedEventArgs e)
    {
        _vm?.ToggleChapterPanel();
        // 面板展开后自动滚动到当前章节
        if (_vm != null && _vm.IsChapterPanelOpen)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_vm.ChapterIndex >= 0 && _vm.ChapterIndex < ChapterListBox.Items.Count)
                {
                    ChapterListBox.ScrollIntoView(ChapterListBox.Items[_vm.ChapterIndex]);
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
    }

    private void CloseChapterPanel_Click(object sender, RoutedEventArgs e)
    {
        if (_vm != null) _vm.IsChapterPanelOpen = false;
    }

    private void ChapterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null || ChapterListBox.SelectedIndex < 0) return;
        // 仅用户点击选择时跳转，忽略程序化选中变化
        if (ChapterListBox.SelectedIndex != _vm.ChapterIndex)
        {
            _vm.JumpToChapter(ChapterListBox.SelectedIndex);
        }
    }

    /// <summary>根据滚动位置计算当前屏幕可见首行对应的字符偏移</summary>
    private int GetVisibleCharOffset()
    {
        if (_vm == null || string.IsNullOrEmpty(_vm.ChapterContent)) return 0;
        if (ReadingScrollViewer.ExtentHeight <= 0) return 0;

        double ratio = ReadingScrollViewer.VerticalOffset / ReadingScrollViewer.ExtentHeight;
        int offset = (int)(ratio * _vm.ChapterContent.Length);
        offset = Math.Clamp(offset, 0, _vm.ChapterContent.Length - 1);

        // 对齐到行首，避免从半个词开始读
        int lineStart = _vm.ChapterContent.LastIndexOf('\n', offset);
        if (lineStart >= 0) offset = lineStart + 1;

        return offset;
    }
}
