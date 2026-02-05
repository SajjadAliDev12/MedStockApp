using MedStock.Services.Interfaces;
using MedStock.UI;
using System;

namespace MedStock.UI.ViewModels
{
    public sealed class MainWindowViewModel : ViewModelBase
    {
        private readonly INavigationService _nav;
        private readonly ISessionContext _session;
        private ViewModelBase _current;
        private bool _isLoggedIn;
        private readonly ILanguageService _lang;
        public MainWindowViewModel(INavigationService nav, ISessionContext session,ILanguageService lang)
        {
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _lang = lang;
            _current = new PlaceholderViewModel();

            // 1. ربط تغيير الصفحة
            _nav.CurrentChanged += vm => CurrentViewModel = vm;

            // 2. مراقبة حالة تسجيل الدخول لإظهار/إخفاء القائمة الجانبية
            if (session is MedStock.Services.Implementations.SessionContext sc)
            {
                sc.SessionChanged += () =>
                {
                    IsLoggedIn = _session.IsAuthenticated;
                    if (IsLoggedIn) _nav.NavigateTo<DashboardViewModel>();
                    else _nav.NavigateTo<LoginViewModel>();
                };
            }

            // تعريف أوامر القائمة الجانبية
            NavHomeCommand = new RelayCommand(() => _nav.NavigateTo<DashboardViewModel>());
            NavItemsCommand = new RelayCommand(() => _nav.NavigateTo<ItemsViewModel>());
            NavStockInCommand = new RelayCommand(() => _nav.NavigateTo<StockInViewModel>());
            NavStockOutCommand = new RelayCommand(() => _nav.NavigateTo<StockOutViewModel>());
            NavSuppliersCommand = new RelayCommand(() => _nav.NavigateTo<SuppliersViewModel>());
            NavStocktakesCommand = new RelayCommand(() => _nav.NavigateTo<StocktakesViewModel>());
            NavReportsCommand = new RelayCommand(() => _nav.NavigateTo<StockCardViewModel>());
            NavUsersCommand = new RelayCommand(() => _nav.NavigateTo<UsersViewModel>());
            NavMinStockCommand = new RelayCommand(() => _nav.NavigateTo<MinStockAlertsViewModel>());
            NavRequisitionsCommand = new RelayCommand(() => _nav.NavigateTo<RequisitionsViewModel>());
            NavCategoriesCommand = new RelayCommand(() => _nav.NavigateTo<CategoriesViewModel>());
            NavItemCategoriesCommand = new RelayCommand(() => _nav.NavigateTo<ItemCategoriesViewModel>());
            NavExpiryReportCommand = new RelayCommand(() => _nav.NavigateTo<ExpiryReportViewModel>());
            NavDepartmentsCommand = new RelayCommand(() => _nav.NavigateTo<DepartmentsViewModel>());
            GoToAuditLogsCommand = new RelayCommand(() => nav.NavigateTo<AuditLogsViewModel>());
            ConsumptionReport = new RelayCommand(() => nav.NavigateTo<ConsumptionReportViewModel>());
            LogoutCommand = new RelayCommand(() => _session.Clear());

            // البداية
            _nav.NavigateTo<LoginViewModel>();
        }

        public ViewModelBase CurrentViewModel
        {
            get => _current;
            private set => SetProperty(ref _current, value);
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            private set => SetProperty(ref _isLoggedIn, value);
        }
        public string MenuRequisitions => _lang.RequisitionLabel;

        // "صرف للأقسام" أو "صرف للعملاء"
        public string MenuStockOut => _lang.ActivityName == "Hospital" ? "صرف مواد (للأقسام)" : "فاتورة مبيعات / صرف";

        // "الموردين" (عادة ثابتة، لكن يمكن تغييرها إذا أردت)
        public string MenuSuppliers => "الموردين";

        // "الأقسام" (إذا أضفنا شاشة إدارة لها لاحقاً)
        public string MenuDepartments => _lang.DepartmentLabel;
        // أوامر القائمة الجانبية
        public RelayCommand NavHomeCommand { get; }
        public RelayCommand GoToAuditLogsCommand { get; }
        public RelayCommand NavItemsCommand { get; }
        public RelayCommand NavExpiryReportCommand { get; }
        public RelayCommand NavStockInCommand { get; }
        public RelayCommand NavStockOutCommand { get; }
        public RelayCommand NavSuppliersCommand { get; }
        public RelayCommand NavStocktakesCommand { get; }
        public RelayCommand NavReportsCommand { get; }
        public RelayCommand NavUsersCommand { get; }
        public RelayCommand ConsumptionReport {  get; }
        public RelayCommand NavMinStockCommand { get; }
        public RelayCommand NavDepartmentsCommand { get; }
        public RelayCommand NavRequisitionsCommand { get; }
        public RelayCommand LogoutCommand { get; }
        public RelayCommand NavCategoriesCommand { get; }
        public RelayCommand NavItemCategoriesCommand { get; }
        private sealed class PlaceholderViewModel : ViewModelBase { }
    }
}