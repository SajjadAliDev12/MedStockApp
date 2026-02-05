using System;
using System.Threading.Tasks;
using MedStock.Services.Interfaces;
using System.Linq;

namespace MedStock.UI.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        private readonly ISessionContext _session;
        private readonly IItemsService _itemsService;
        private readonly IAlertsService _alertsService;
        private readonly IRequisitionsService _reqService;
        private readonly IReportsService _reportsService; // خدمة التقارير الجديدة

        private int _lowStockCount;
        private int _pendingReqCount;
        private int _expiringSoonCount; // عداد جديد
        private string _welcomeText = "";
        private bool _isBusy;

        public DashboardViewModel(
            ISessionContext session,
            IItemsService itemsService,
            IAlertsService alertsService,
            IRequisitionsService reqService,
            IReportsService reportsService) // حقن الخدمة
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _itemsService = itemsService;
            _alertsService = alertsService;
            _reqService = reqService;
            _reportsService = reportsService;
        }

        public string WelcomeText { get => _welcomeText; private set => SetProperty(ref _welcomeText, value); }
        public int LowStockCount { get => _lowStockCount; private set => SetProperty(ref _lowStockCount, value); }
        public int PendingReqCount { get => _pendingReqCount; private set => SetProperty(ref _pendingReqCount, value); }
        public int ExpiringSoonCount { get => _expiringSoonCount; private set => SetProperty(ref _expiringSoonCount, value); }
        public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

        // دالة التحميل
        public async Task InitAsync()
        {
            if (_session.CurrentUser == null)
            {
                WelcomeText = "الرجاء تسجيل الدخول";
                return;
            }

            WelcomeText = $"مرحباً بك، {_session.CurrentUser.DisplayName}";
            IsBusy = true;

            try
            {
                // 1. تنبيهات الحد الأدنى (Low Stock)
                var alerts = await _alertsService.GetMinStockAlertsAsync(null);
                LowStockCount = alerts.Count;

                // 2. الطلبات المعلقة (Submitted) التي تنتظر الموافقة
                // نرسل null للقسم لنجلب طلبات كل الأقسام، ونبحث عن الحالة Submitted
                var reqs = await _reqService.GetListAsync("Submitted", null, null);
                PendingReqCount = reqs.Count;

                // 3. المواد التي ستنتهي صلاحيتها قريباً (خلال 90 يوم مثلاً)
                var expiring = await _reportsService.GetExpiryReportAsync(90);
                ExpiringSoonCount = expiring.Count;
            }
            catch (Exception ex)
            {
                // في الداشبورد نفضل عدم إظهار MessageBox مزعج، ربما نكتب في Console أو Log
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}