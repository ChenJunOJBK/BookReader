using System.Windows;
using YueDu.ViewModels;

namespace YueDu;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
