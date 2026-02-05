using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class RequisitionsViewModel : ViewModelBase
    {
        private readonly IRequisitionsService _service;
        private readonly ISessionContext _session;
        private readonly INavigationService _nav;
        private readonly RequisitionContext _context;
        private bool _isBusy;
        private string _statusMessage = "";
        private readonly ILanguageService _lang;
        private string? _selectedStatus;
        private DepartmentRow? _selectedDepartment;
        private string _searchText = "";

        private RequisitionListRow? _selectedReq;

        public ObservableCollection<string> Statuses { get; } = new();
        public ObservableCollection<DepartmentRow> Departments { get; } = new();
        public ObservableCollection<RequisitionListRow> Requisitions { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    CreateDraftCommand.RaiseCanExecuteChanged();
                    OpenDetailsCommand.RaiseCanExecuteChanged();
                }
            }
        }


        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public string? SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (SetProperty(ref _selectedStatus, value))
                    _ = RefreshAsync();
            }
        }

        public DepartmentRow? SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                    _ = RefreshAsync();CreateDraftCommand.RaiseCanExecuteChanged();
            }
        }
        

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public RequisitionListRow? SelectedRequisition
        {
            get => _selectedReq;
            set
            {
                if (SetProperty(ref _selectedReq, value))
                    OpenDetailsCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand CreateDraftCommand { get; }
        public RelayCommand OpenDetailsCommand { get; }

        public RequisitionsViewModel(IRequisitionsService service, ISessionContext session, INavigationService nav, RequisitionContext context , ILanguageService lang)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
            _context = context;
            _lang = lang;
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            CreateDraftCommand = new RelayCommand(
    async () => await CreateDraftAsync(),
    () => !IsBusy && SelectedDepartment != null && SelectedDepartment.DepartmentId > 0
);
            OpenDetailsCommand = new RelayCommand(OpenDetails, () => !IsBusy && SelectedRequisition != null);

            SeedStatuses();
        }
        public string DeptLabel => _lang.DepartmentLabel;
        public string TitleLabel => _lang.RequisitionLabel;
        private void SeedStatuses()
        {
            Statuses.Clear();
            Statuses.Add(""); // all
            Statuses.Add("Draft");
            Statuses.Add("Submitted");
            Statuses.Add("Approved");
            Statuses.Add("Rejected");
            Statuses.Add("Fulfilled");
            Statuses.Add("Cancelled");
        }

        public async Task InitAsync()
        {
            // نظهر الانتظار أثناء تحميل الأقسام
            IsBusy = true;
            StatusMessage = "جاري تهيئة البيانات...";
            try
            {
                Departments.Clear();
                Departments.Add(new DepartmentRow { DepartmentId = 0, DepartmentName = "الكل" });

                var depts = await _service.GetDepartmentsAsync();
                foreach (var d in depts) Departments.Add(d);

                // هام جداً: نستخدم المتغير الداخلي (_) لتجنب تفعيل الـ Setter 
                // الذي يقوم باستدعاء RefreshAsync بشكل غير متزامن ويسبب مشاكل
                _selectedDepartment = Departments.Count > 0 ? Departments[0] : null;
                OnPropertyChanged(nameof(SelectedDepartment));

                // هام جداً: يجب إيقاف حالة المشغول قبل استدعاء RefreshAsync
                // وإلا فإن RefreshAsync سيتوقف فوراً لأنه يظن أن هناك عملية أخرى جارية
                IsBusy = false;

                // الآن نستدعي التحديث يدوياً ومباشرة
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
                IsBusy = false; // تأكد من إغلاق المشغول في حال الخطأ
            }
        }

        public async Task RefreshAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            StatusMessage = "";
            try
            {
                Requisitions.Clear();
                var deptId = (SelectedDepartment != null && SelectedDepartment.DepartmentId > 0)
                    ? SelectedDepartment.DepartmentId
                    : (int?)null;

                var list = await _service.GetListAsync(
                    status: string.IsNullOrWhiteSpace(SelectedStatus) ? null : SelectedStatus,
                    departmentId: deptId,
                    search: string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
                );

                foreach (var r in list) Requisitions.Add(r);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task CreateDraftAsync()
        {
            IsBusy = true;
            StatusMessage = "";

            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser is null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                if (SelectedDepartment is null || SelectedDepartment.DepartmentId <= 0)
                    throw new InvalidOperationException("اختر القسم أولاً.");

                var id = await _service.CreateDraftAsync(
                    departmentId: SelectedDepartment.DepartmentId,
                    notes: null,
                    requestedByUserId: _session.CurrentUser.UserId
                );

                await RefreshAsync();
                _context.CurrentRequisitionId = id;
                _nav.NavigateTo<RequisitionDetailsViewModel>();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OpenDetails()
        {
            if (SelectedRequisition != null)
            {
                // 1. نضع الرقم في الصندوق المشترك
                _context.CurrentRequisitionId = SelectedRequisition.RequisitionId;

                // 2. ننتقل للصفحة (وهي ستقرأ الصندوق عند التحميل)
                _nav.NavigateTo<RequisitionDetailsViewModel>();
            }

        }
    }
}
