using System.Windows;
using System.Windows.Controls;

namespace MedStock.UI.Views
{
    // الطباعة هنا محلية على مستوى العرض (View-local): تطبع الشكل المرئي الحالي كما هو.
    public partial class MinStockAlertsView : UserControl
    {
        public MinStockAlertsView()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is MedStock.UI.ViewModels.MinStockAlertsViewModel vm)
                    await vm.RefreshAsync();
            };
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
                dlg.PrintVisual(this, "تنبيهات النواقص");
        }
    }
}
