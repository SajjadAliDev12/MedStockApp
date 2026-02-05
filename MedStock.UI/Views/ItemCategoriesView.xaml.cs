using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class ItemCategoriesView : UserControl
    {
        public ItemCategoriesView()
        {
            InitializeComponent();

            // Load once after view is created (keeps MVVM simple)
            Loaded += async (_, __) =>
            {
                if (DataContext is MedStock.UI.ViewModels.ItemCategoriesViewModel vm)
                    await vm.InitAsync();
            };
        }
    }
}
