using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YueDu.Models;
using YueDu.ViewModels;

namespace YueDu.Views;

public partial class BookShelfView : UserControl
{
    private BookShelfViewModel VM => (BookShelfViewModel)DataContext;

    public BookShelfView()
    {
        InitializeComponent();
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        VM.ImportBooks();
    }

    private void Book_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is Book book)
        {
            VM.OpenBook(book);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is FrameworkElement fe && fe.Tag is Book book)
        {
            VM.DeleteBook(book);
        }
    }
}
