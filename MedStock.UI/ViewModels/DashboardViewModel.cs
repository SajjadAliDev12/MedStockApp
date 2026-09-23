using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.Implementations;
using MedStock.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace MedStock.UI.ViewModels
{
    public sealed class DashboardViewModel : ViewModelBase
    {
        public sealed class TopConsumerRow
        {
            public string ItemName { get; set; } = "";
            public decimal TotalQty { get; set; }
            public double BarWidth { get; set; }
        }

        private readonly ISessionContext _session;
        private readonly IItemsService _itemsService;
        private readonly IAlertsService _alertsService;
        private readonly IRequisitionsService _reqService;
        private readonly IReportsService _reportsService; // خدمة التقارير الجديدة
        private readonly IEmailService _emailService;
        private readonly string _adminEmail;

        private int _lowStockCount;
        private int _pendingReqCount;
        private int _expiringSoonCount; // عداد جديد
        private string _welcomeText = "";
        private string _monthTotal = "";
        private string _statusMessage = "";
        private bool _isBusy;
        private ObservableCollection<TopConsumerRow> _topConsumers = new();

        public DashboardViewModel(
            ISessionContext session,
            IItemsService itemsService,
            IAlertsService alertsService,
            IRequisitionsService reqService,
            IReportsService reportsService, // حقن الخدمة
            IConfiguration config)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _itemsService = itemsService;
            _alertsService = alertsService;
            _reqService = reqService;
            _reportsService = reportsService;
            // لا يتطلب تسجيل DI: نبني الخدمة من IConfiguration المسجل تلقائياً في الـ Host
            _emailService = EmailService.FromConfiguration(config ?? throw new ArgumentNullException(nameof(config)));
            _adminEmail = config["Email:AdminEmail"] ?? "";
            SendAlertsDigestCommand = new RelayCommand(async () => await SendAlertsDigestAsync(), () => !IsBusy);
        }

        public string WelcomeText { get => _welcomeText; private set => SetProperty(ref _welcomeText, value); }
        public int LowStockCount { get => _lowStockCount; private set => SetProperty(ref _lowStockCount, value); }
        public int PendingReqCount { get => _pendingReqCount; private set => SetProperty(ref _pendingReqCount, value); }
        public int ExpiringSoonCount { get => _expiringSoonCount; private set => SetProperty(ref _expiringSoonCount, value); }
        public ObservableCollection<TopConsumerRow> TopConsumers { get => _topConsumers; private set => SetProperty(ref _topConsumers, value); }
        public string MonthTotal { get => _monthTotal; private set => SetProperty(ref _monthTotal, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

        public RelayCommand SendAlertsDigestCommand { get; }

        private async Task SendAlertsDigestAsync()
        {
            IsBusy = true;
            try
            {
                if (!_emailService.IsConfigured)
                {
                    StatusMessage = "تم التخطي: مفتاح البريد (ApiKey) غير مُعد.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(_adminEmail))
                {
                    StatusMessage = "تم التخطي: بريد المشرف (AdminEmail) غير مُعد.";
                    return;
                }

                var ok = await _emailService.SendAlertsDigestAsync(_adminEmail.Trim(), LowStockCount, ExpiringSoonCount, PendingReqCount);
                StatusMessage = ok ? "تم إرسال ملخص التنبيهات بنجاح." : "فشل إرسال البريد.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في الإرسال: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

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

                // 4. الأعلى استهلاكاً هذا الشهر (أول 5 مواد)
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var summary = await _reportsService.GetConsumptionSummaryAsync(monthStart, now, null, null);
                var top = summary.OrderByDescending(x => x.TotalQty).Take(5).ToList();
                var max = top.Count == 0 ? 0m : top.Max(x => x.TotalQty);
                TopConsumers.Clear();
                foreach (var r in top)
                {
                    TopConsumers.Add(new TopConsumerRow
                    {
                        ItemName = r.ItemName,
                        TotalQty = r.TotalQty,
                        BarWidth = max <= 0 ? 0 : (double)(r.TotalQty / max * 220)
                    });
                }
                MonthTotal = $"إجمالي المصروف هذا الشهر: {summary.Sum(x => x.TotalQty):N0}";
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