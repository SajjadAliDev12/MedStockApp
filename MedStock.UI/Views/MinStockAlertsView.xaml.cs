using System.Windows.Controls;

namespace MedStock.UI.Views
{
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
    }
}
