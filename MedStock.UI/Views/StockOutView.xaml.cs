using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class StockOutView : UserControl
    {
        public StockOutView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.StockOutViewModel vm)
                {
                    await vm.LoadDataAsync();
                }
            };
        }
    }
}
