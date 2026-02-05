using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class StocktakeDetailsView : UserControl
    {
        public StocktakeDetailsView()
        {
            InitializeComponent();

            // هذا الكود هو المسؤول عن ملء القائمة عند فتح الشاشة
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.StocktakeDetailsViewModel vm)
                {
                    await vm.InitAsync();
                }
            };
        }
    }
}