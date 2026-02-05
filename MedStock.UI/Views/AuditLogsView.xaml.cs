using System.Windows.Controls;

namespace MedStock.UI.Views
{
    public partial class AuditLogsView : UserControl
    {
        public AuditLogsView()
        {
            InitializeComponent();

            // الاستماع لحدث التحميل لاستدعاء التهيئة في الـ ViewModel
            Loaded += async (s, e) =>
            {
                if (DataContext is MedStock.UI.ViewModels.AuditLogsViewModel vm)
                {
                    // بما أن الـ VM سنجلتون، هذه الدالة ستُستدعى في كل مرة تفتح الصفحة
                    // المنطق داخل الدالة هو الذي يقرر هل يعيد التحميل أم يكتفي بالبيانات الحالية
                    await vm.InitAsync();
                }
            };
        }
    }
}