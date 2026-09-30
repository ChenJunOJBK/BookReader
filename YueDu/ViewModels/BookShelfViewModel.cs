using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;
using YueDu.Models;
using YueDu.Services;

namespace YueDu.ViewModels;

/// <summary>
/// 书架视图模型
/// </summary>
public class BookShelfViewModel : INotifyPropertyChanged
{
    private readonly MainViewModel _main;

    public ObservableCollection<Book> Books { get; } = new();

    public BookShelfViewModel(MainViewModel main)
    {
        _main = main;
        Refresh();
    }

    public void Refresh()
    {
        Books.Clear();
        foreach (var b in _main.BookService.Books)
            Books.Add(b);
    }

    /// <summary>导入TXT文件</summary>
    public void ImportBooks()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            Multiselect = true,
            Title = "选择要导入的小说TXT文件"
        };

        if (dlg.ShowDialog() == true)
        {
            foreach (var file in dlg.FileNames)
            {
                var book = _main.BookService.ImportBook(file);
                if (book != null)
                {
                    // 插入到最前
                    Books.Insert(0, book);
                }
            }
        }
    }

    /// <summary>打开书籍阅读</summary>
    public void OpenBook(Book book)
    {
        if (book != null)
            _main.OpenReader(book);
    }

    /// <summary>删除书籍</summary>
    public void DeleteBook(Book book)
    {
        if (book == null) return;
        var result = MessageBox.Show($"确定要删除《{book.Title}》吗？", "确认删除",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            _main.BookService.RemoveBook(book);
            Books.Remove(book);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
