using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MedStock.UI.Views
{
    /// <summary>
    /// Interaction logic for ExpiryReportView.xaml
    /// </summary>
    public partial class ExpiryReportView : UserControl
    {
        public ExpiryReportView()
        {
            InitializeComponent();
            Loaded += async (e, s) =>
            {
                if (DataContext is MedStock.UI.ViewModels.ExpiryReportViewModel vm)
                {
                   await vm.LoadDataAsync();
                }
            };
        }
    }
}
