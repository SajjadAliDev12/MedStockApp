using System.Windows;
using MedStock.UI.ViewModels;

namespace MedStock.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm)
        {
            InitializeComponent();
            
            DataContext = vm;
        }
    }
}
