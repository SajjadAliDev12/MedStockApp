using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class StockInView : UserControl
    {
        public StockInView()
        {
            InitializeComponent();

            // هذا الحدث سيقوم بتحميل القوائم عند ظهور الشاشة
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.StockInViewModel vm)
                {
                    await vm.InitAsync();
                }
            };
        }
    }
}