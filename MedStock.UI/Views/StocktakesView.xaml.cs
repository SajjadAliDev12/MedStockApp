using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class StocktakesView : UserControl
    {
        public StocktakesView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.StocktakesViewModel vm)
                    await vm.InitAsync();
            };
        }
    }
}