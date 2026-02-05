using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MedStock.UI.Views
{
    public partial class RequisitionDetailsView : UserControl
    {
        public RequisitionDetailsView()
        {
            InitializeComponent();

            
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.RequisitionDetailsViewModel vm)
                {
                    await vm.InitAsync(); 
                }
            };
        }
    }

}
