using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class AuditLogsViewModel : ViewModelBase
    {
        private readonly IAuditService _service;
        private readonly INavigationService _nav;

        // حالة الصفحة (Busy / Status)
        private bool _isBusy;
        private string _statusMessage = "";

        // الفلاتر (Filters)
        private DateTime? _dateFrom = DateTime.Today.AddDays(-7); // افتراضياً آخر أسبوع
        private DateTime? _dateTo = DateTime.Today;
        private int? _selectedUserId;
        private string? _selectedAction;
        private string _searchText = "";

        // متغيرات الصفحات (Pagination Fields)
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalRecords = 0;
        private readonly int _pageSize = 50; // عدد السجلات في كل صفحة

        // القوائم
        public ObservableCollection<AuditLogListRow> Logs { get; } = new();
        public ObservableCollection<IdNameRow> UsersLookup { get; } = new();
        public ObservableCollection<string> ActionsLookup { get; } = new();

        public AuditLogsViewModel(IAuditService service, INavigationService nav)
        {
            _service = service;
            _nav = nav;

            // أوامر البحث والتصفية
            // true تعني ابدأ البحث من الصفحة الأولى
            SearchCommand = new RelayCommand(async () => await SearchAsync(true));
            ClearFiltersCommand = new RelayCommand(ClearFilters);

            // أوامر التنقل بين الصفحات
            NextPageCommand = new RelayCommand(async () => await ChangePage(1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await ChangePage(-1), () => CurrentPage > 1);
        }

        // --- Properties ---
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        // خصائص الفلترة
        public DateTime? DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }
        public DateTime? DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }
        public int? SelectedUserId { get => _selectedUserId; set => SetProperty(ref _selectedUserId, value); }
        public string? SelectedAction { get => _selectedAction; set => SetProperty(ref _selectedAction, value); }
        public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

        // خصائص الصفحات
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    OnPropertyChanged(nameof(PageInfo));
                    UpdatePaginationCommands();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                if (SetProperty(ref _totalPages, value))
                {
                    OnPropertyChanged(nameof(PageInfo));
                    UpdatePaginationCommands();
                }
            }
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        // خاصية للعرض في الواجهة (مثال: صفحة 1 من 10)
        public string PageInfo => $"صفحة {CurrentPage} من {TotalPages} (العدد الكلي: {TotalRecords})";

        // --- Commands ---
        public RelayCommand SearchCommand { get; }
        public RelayCommand ClearFiltersCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PrevPageCommand { get; }

        // --- Methods ---

        // يتم استدعاؤها من الـ Code-Behind (Loaded event)
        public async Task InitAsync()
        {
            // بما أن الـ VM سنجلتون، نتحقق هل القوائم محملة مسبقاً لتجنب التكرار
            if (UsersLookup.Count == 0 || ActionsLookup.Count == 0)
            {
                await LoadLookupsAsync();
            }

            
            await SearchAsync(false);
        }

        private async Task LoadLookupsAsync()
        {
            try
            {
                IsBusy = true;

                var users = await _service.GetUsersLookupAsync();
                UsersLookup.Clear();
                foreach (var u in users) UsersLookup.Add(u);

                var actions = await _service.GetActionTypesAsync();
                ActionsLookup.Clear();
                foreach (var a in actions) ActionsLookup.Add(a);
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل تحميل القوائم: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // دالة البحث الرئيسية (تم تعديلها لدعم الصفحات)
        private async Task SearchAsync(bool resetPage = false)
        {
            if (resetPage) CurrentPage = 1;

            IsBusy = true;
            StatusMessage = "جاري البحث...";
            try
            {
                var filter = new AuditLogFilter
                {
                    FromDate = DateFrom,
                    ToDate = DateTo,
                    UserId = SelectedUserId,
                    ActionType = SelectedAction,
                    SearchText = SearchText,
                    // تمرير معلومات الصفحة
                    PageNumber = CurrentPage,
                    PageSize = _pageSize
                };

                // استدعاء الخدمة التي ترجع PagedResult
                var result = await _service.SearchLogsAsync(filter);

                Logs.Clear();
                foreach (var row in result.Items) Logs.Add(row);

                // تحديث عدادات الصفحات
                TotalRecords = result.TotalCount;
                TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;

                // تحديث نص المعلومات وحالة الأزرار
                OnPropertyChanged(nameof(PageInfo));
                UpdatePaginationCommands();

                StatusMessage = $"تم التحميل ({result.Items.Count} سجل في هذه الصفحة).";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في البحث: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ChangePage(int offset)
        {
            CurrentPage += offset;
            await SearchAsync(false); // false = حافظ على رقم الصفحة الجديد ولا تعيد التصفير
        }

        private void UpdatePaginationCommands()
        {
            NextPageCommand.RaiseCanExecuteChanged();
            PrevPageCommand.RaiseCanExecuteChanged();
        }

        private void ClearFilters()
        {
            DateFrom = null;
            DateTo = null;
            SelectedUserId = null;
            SelectedAction = null;
            SearchText = "";

            // إعادة ضبط الصفحات
            CurrentPage = 1;
            TotalPages = 1;
            TotalRecords = 0;
            Logs.Clear();

            StatusMessage = "تم مسح الفلاتر.";
        }
    }
}