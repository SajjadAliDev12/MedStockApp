using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class ItemsView : UserControl // مثال على صفحة المواد
    {
        public ItemsView()
        {
            InitializeComponent();

            // هذا هو السطر السحري الذي سيقوم بالتحميل التلقائي
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.ItemsViewModel vm)
                {
                    await vm.InitAsync();
                }
            };
        }
    }
}
