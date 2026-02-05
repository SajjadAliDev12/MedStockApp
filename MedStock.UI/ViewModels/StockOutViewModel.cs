using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;


namespace MedStock.UI.ViewModels
{
    public sealed class StockOutViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventory;
        private readonly IRequisitionsService _reqService;
        private readonly IItemsService _itemsService;
        private readonly ISessionContext _session;

        // القوائم
        public ObservableCollection<DepartmentRow> Departments { get; } = new();
        public ObservableCollection<ItemListRow> SearchResults { get; } = new();
        public ObservableCollection<PendingRequisitionRow> PendingRequests { get; } = new();

        // المدخلات
        private ItemListRow _selectedItem;
        private DepartmentRow _selectedDepartment;
        private PendingRequisitionRow _selectedPendingRequest;

        private string _itemSearchText = "";
        private decimal _qty = 0;
        private string _reasonCode = TransactionReasons.ReqFulfill;
        private string _notes = "";

        private string _statusMessage = "";
        private bool _isBusy;
        private bool _isManualMode = true;

        // خاصية جديدة للتحكم بفتح القائمة
        private bool _isDropDownOpen;

        // للتحكم في تأخير البحث (Debounce)
        private CancellationTokenSource _searchCts;

        public StockOutViewModel(
            IInventoryService inventory,
            IRequisitionsService reqService,
            IItemsService itemsService,
            ISessionContext session)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _reqService = reqService ?? throw new ArgumentNullException(nameof(reqService));
            _itemsService = itemsService ?? throw new ArgumentNullException(nameof(itemsService));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            LoadCommand = new RelayCommand(async () => await LoadDataAsync());
            SearchItemCommand = new RelayCommand(async () => await SearchItemsAsync());
            SubmitCommand = new RelayCommand(async () => await SubmitAsync(), () => !IsBusy);
            SelectPendingCommand = new RelayCommand<PendingRequisitionRow>(req => SelectPendingRequest(req));
            ClearCommand = new RelayCommand(() => ClearForm());
        }

        // --- Properties ---

        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            set => SetProperty(ref _isDropDownOpen, value);
        }

        public bool IsManualMode
        {
            get => _isManualMode;
            set { SetProperty(ref _isManualMode, value); ClearForm(keepMode: true); }
        }

        public string ItemSearchText
        {
            get => _itemSearchText;
            set
            {
                if (SetProperty(ref _itemSearchText, value))
                {
                    // إذا اختار المستخدم عنصراً من القائمة، لا نريد إعادة البحث
                    if (SelectedItem != null && value == SelectedItem.Name) return;

                    // إلغاء البحث السابق إذا كان المستخدم يكتب بسرعة
                    _searchCts?.Cancel();
                    _searchCts = new CancellationTokenSource();

                    if (!string.IsNullOrWhiteSpace(value) && value.Length >= 2)
                    {
                        // تشغيل البحث بتأخير بسيط (300ms)
                        var token = _searchCts.Token;
                        Task.Delay(300, token).ContinueWith(async _ =>
                        {
                            if (token.IsCancellationRequested) return;
                            // العودة للـ UI Thread للبحث
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => SearchItemsAsync());
                        });
                    }
                    else if (string.IsNullOrWhiteSpace(value))
                    {
                        SearchResults.Clear();
                        IsDropDownOpen = false;
                    }
                }
            }
        }

        public ItemListRow SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    // عند اختيار عنصر، أغلق القائمة وضع الاسم في مربع البحث
                    if (value != null)
                    {
                        // استخدام متغير مؤقت لتجنب تفعيل البحث مرة أخرى
                        _itemSearchText = value.Name;
                        OnPropertyChanged(nameof(ItemSearchText));
                        IsDropDownOpen = false;
                    }
                }
            }
        }

        // ... بقية الـ Properties كما هي (SelectedDepartment, Qty, ReasonCode, Notes, StatusMessage, IsBusy) ...
        public DepartmentRow SelectedDepartment { get => _selectedDepartment; set => SetProperty(ref _selectedDepartment, value); }
        public decimal Qty { get => _qty; set => SetProperty(ref _qty, value); }
        public string ReasonCode { get => _reasonCode; set => SetProperty(ref _reasonCode, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) ((RelayCommand)SubmitCommand).RaiseCanExecuteChanged(); } }

        public ICommand LoadCommand { get; }
        public ICommand SearchItemCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand SelectPendingCommand { get; }
        public ICommand ClearCommand { get; }

        public async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                var depts = await _reqService.GetDepartmentsAsync();
                Departments.Clear();
                foreach (var d in depts) Departments.Add(d);
                await RefreshPendingList();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task RefreshPendingList()
        {
            var pending = await _reqService.GetPendingToIssueAsync();
            PendingRequests.Clear();
            foreach (var p in pending) PendingRequests.Add(p);
        }

        private async Task SearchItemsAsync()
        {
            if (string.IsNullOrWhiteSpace(ItemSearchText) || ItemSearchText.Length < 2) return;

            try
            {
                var items = await _itemsService.GetItemsAsync(ItemSearchText);
                SearchResults.Clear();
                foreach (var i in items) SearchResults.Add(i);

                // الحل السحري: افتح القائمة إذا وجدت نتائج
                if (SearchResults.Count > 0)
                {
                    IsDropDownOpen = true;
                }
                else
                {
                    IsDropDownOpen = false;
                    // اختياري: إضافة عنصر وهمي "لا توجد نتائج"
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل البحث: " + ex.Message;
                IsDropDownOpen = false;
            }
        }

        private void SelectPendingRequest(PendingRequisitionRow req)
        {
            if (req == null) return;
            IsManualMode = false;          // هذا سيعمل ClearForm
            _selectedPendingRequest = req; // ✅ أعد تعيينه بعد ClearForm

            // تعبئة البيانات
            // ملاحظة: هنا ننشئ كائناً جديداً لكي يظهر الاسم، لكنه لا يؤثر على القائمة المنسدلة
            _selectedItem = new ItemListRow { ItemId = req.ItemId, Name = req.ItemName };
            OnPropertyChanged(nameof(SelectedItem));

            _itemSearchText = req.ItemName;
            OnPropertyChanged(nameof(ItemSearchText));

            SelectedDepartment = Departments.FirstOrDefault(d => d.DepartmentName == req.DepartmentName);
            Qty = req.RemainingQty;
            ReasonCode = TransactionReasons.ReqFulfill;
            StatusMessage = $"تم اختيار الطلب {req.RequisitionNo}.";
        }

        private async Task SubmitAsync()
        {
            if (SelectedItem == null) { StatusMessage = "يجب اختيار مادة."; return; }
            if (Qty <= 0) { StatusMessage = "الكمية خطأ."; return; }

            IsBusy = true;
            try
            {
                var request = new StockOutRequest
                {
                    CreatedByUserId = _session.CurrentUser.UserId,
                    ItemId = SelectedItem.ItemId,
                    Quantity = Qty,
                    Notes = Notes,
                    DepartmentId = SelectedDepartment?.DepartmentId,
                    RequisitionDetailId = _selectedPendingRequest?.RequisitionDetailId,
                    ReasonCode = _selectedPendingRequest != null ? TransactionReasons.ReqFulfill : ReasonCode
                };
                if (request.RequisitionDetailId != null)
                {
                    var trxId = await _reqService.FulfillLineAsync((long)request.RequisitionDetailId, request.Quantity, request.CreatedByUserId, request.DepartmentId, request.Notes);
                    StatusMessage = $"تم الصرف: {trxId}";
                    ClearForm();
                    await RefreshPendingList();
                }
                else
                {
                    var trxId = await _inventory.StockOutAsync(request);
                    StatusMessage = $"تم الصرف: {trxId}";
                    ClearForm();
                    await RefreshPendingList();
                }
            }
            catch (Exception ex) { StatusMessage = "خطأ: " + ex.Message; }
            finally { IsBusy = false; }
        }

        private void ClearForm(bool keepMode = false)
        {
            SelectedItem = null;
            ItemSearchText = "";
            SearchResults.Clear();
            IsDropDownOpen = false; // إغلاق القائمة
            if (!keepMode) IsManualMode = true;
            _selectedPendingRequest = null;
            SelectedDepartment = null;
            Qty = 0;
            Notes = "";
            ReasonCode = TransactionReasons.ReqFulfill;
            StatusMessage = "جاهز.";
        }
    }
}