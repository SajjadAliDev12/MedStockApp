using System.Windows;
using System.Windows.Controls;

namespace MedStock.UI.Views
{
    // الطباعة هنا محلية على مستوى العرض (View-local): تطبع الشكل المرئي الحالي كما هو.
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

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
                dlg.PrintVisual(this, "بطاقة المادة");
        }
    }
}