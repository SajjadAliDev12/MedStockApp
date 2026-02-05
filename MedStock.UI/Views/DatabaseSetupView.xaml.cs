using System.Windows;
using System.Windows.Controls;
using MedStock.UI.ViewModels; // تأكد من استدعاء الـ Namespace

namespace MedStock.UI.Views
{
    public partial class DatabaseSetupView : Window
    {
        public DatabaseSetupView()
        {
            InitializeComponent();
        }

        // هذا الحدث ينقل كلمة المرور من الصندوق إلى الـ ViewModel
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is DatabaseSetupViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}