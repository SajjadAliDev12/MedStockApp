using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class ConsumptionReportView : UserControl
    {
        public ConsumptionReportView()
        {
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.ConsumptionReportViewModel vm)
                {
                    await vm.InitAsync();
                }
            };
        }
    }
}