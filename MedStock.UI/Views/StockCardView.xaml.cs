using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class StockCardView : UserControl
    {
        public StockCardView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.StockCardViewModel vm)
                    await vm.InitAsync();
            };
        }
    }
}