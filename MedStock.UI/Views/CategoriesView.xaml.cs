using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class CategoriesView : UserControl
    {
        public CategoriesView()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                if (DataContext is MedStock.UI.ViewModels.CategoriesViewModel vm)
                    await vm.InitAsync();
            };
        }
    }
}
