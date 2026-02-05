using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class RequisitionDetailsViewModel : ViewModelBase
    {
        private readonly IRequisitionsService _service;
        private readonly IItemsService _items;
        private readonly ISessionContext _session;
        private readonly RequisitionContext _context;
        private readonly ILanguageService _lang;
        private bool _isBusy;
        private string _statusMessage = "";
        private readonly INavigationService _nav;
        private long _requisitionId;

        // Header Fields
        private string _requisitionNo = "";
        private string _status = "";
        private DepartmentRow? _selectedDepartment;
        private string _notes = "";

        // Line Editor Fields
        private IdNameRow? _selectedItem;
        private string _qtyText = "1";
        private string _lineNotes = "";

        private RequisitionDetailRow? _selectedLine;

        public ObservableCollection<DepartmentRow> Departments { get; } = new();
        public ObservableCollection<IdNameRow> ItemsLookup { get; } = new();
        public ObservableCollection<RequisitionDetailRow> Lines { get; } = new();

        public RequisitionDetailsViewModel(
            IRequisitionsService service,
            IItemsService items,
            ISessionContext session,
            RequisitionContext context, ILanguageService lang, INavigationService nav)
        {
            _lang = lang;
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _context = context ?? throw new ArgumentNullException(nameof(context));

            // تعريف الأوامر
            AddLineCommand = new RelayCommand(async () => await AddLineAsync(), () => CanEdit && !IsBusy);
            RemoveLineCommand = new RelayCommand<long>(async (id) => await RemoveLineAsync(id), _ => CanEdit && !IsBusy);

            SubmitCommand = new RelayCommand(async () => await SubmitAsync(), () => CanEdit && !IsBusy);
            ApproveCommand = new RelayCommand(async () => await ApproveAsync(), () => CanApprove && !IsBusy);
            RejectCommand = new RelayCommand(async () => await RejectAsync(), () => CanApprove && !IsBusy);
            CancelCommand = new RelayCommand(async () => await CancelAsync(), () => CanCancel && !IsBusy);
            BackCommand = new RelayCommand(GoBack, () => !IsBusy);
            FulfillLineCommand = new RelayCommand(async () => await FulfillLineAsync(), () => CanFulfill && !IsBusy && SelectedLine != null);
            _nav = nav ?? throw new ArgumentNullException(nameof(nav));
        }

        // --- Properties ---

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    // تحديث حالة الأزرار عند الانشغال
                    SubmitCommand.RaiseCanExecuteChanged();
                    AddLineCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public string RequisitionNo { get => _requisitionNo; set => SetProperty(ref _requisitionNo, value); }

        public string Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    // تحديث صلاحيات الأزرار عند تغير الحالة
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanApprove));
                    OnPropertyChanged(nameof(CanCancel));
                    OnPropertyChanged(nameof(CanFulfill));

                    SubmitCommand.RaiseCanExecuteChanged();
                    ApproveCommand.RaiseCanExecuteChanged();
                    RejectCommand.RaiseCanExecuteChanged();
                    CancelCommand.RaiseCanExecuteChanged();
                    AddLineCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public DepartmentRow? SelectedDepartment
        {
            get => _selectedDepartment;
            set => SetProperty(ref _selectedDepartment, value);
        }

        // Editor
        public IdNameRow? SelectedItem { get => _selectedItem; set => SetProperty(ref _selectedItem, value); }
        public string QtyText { get => _qtyText; set => SetProperty(ref _qtyText, value); }
        public string LineNotes { get => _lineNotes; set => SetProperty(ref _lineNotes, value); }

        public RequisitionDetailRow? SelectedLine
        {
            get => _selectedLine;
            set
            {
                if (SetProperty(ref _selectedLine, value))
                {
                    FulfillLineCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Permissions Logic ---
        // التعديل مسموح فقط إذا كانت مسودة
        public bool CanEdit => string.Equals(Status, "Draft", StringComparison.OrdinalIgnoreCase);

        // الاعتماد مسموح إذا تم الإرسال
        public bool CanApprove => string.Equals(Status, "Submitted", StringComparison.OrdinalIgnoreCase);

        // الإلغاء مسموح للمسودة والمرسل
        public bool CanCancel => CanEdit || CanApprove;

        // الصرف مسموح للمعتمد أو المصروف جزئياً
        public bool CanFulfill => string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(Status, "PartiallyFulfilled", StringComparison.OrdinalIgnoreCase);


        // --- Commands ---
        public RelayCommand AddLineCommand { get; }
        public RelayCommand<long> RemoveLineCommand { get; }
        public RelayCommand SubmitCommand { get; }
        public RelayCommand ApproveCommand { get; }
        public RelayCommand RejectCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand FulfillLineCommand { get; }
        public RelayCommand BackCommand { get; }

        // --- Methods ---
        private void GoBack()
        {
            _nav.NavigateTo<RequisitionsViewModel>();
        }
        // هذه هي الدالة المصححة
        public async Task InitAsync()
        {
            // 1. قراءة الرقم من السياق
            var id = _context.CurrentRequisitionId;
            if (id <= 0) return;

            _requisitionId = id;

            IsBusy = true;
            StatusMessage = "جاري تحميل بيانات الطلب...";

            try
            {
                // تحميل القوائم (الأقسام والمواد)
                if (ItemsLookup.Count == 0)
                {
                    var items = await _items.GetItemsAsync(null);
                    foreach (var i in items) ItemsLookup.Add(new IdNameRow { Id = i.ItemId, Name = i.Name, IsActive = i.IsActive });
                }

                if (Departments.Count == 0)
                {
                    var deps = await _service.GetDepartmentsAsync();
                    foreach (var d in deps) Departments.Add(d);
                }

                // تحميل تفاصيل الطلب
                var (header, details) = await _service.GetDetailsAsync(_requisitionId);

                // تعبئة البيانات
                RequisitionNo = header.RequisitionNo;
                Status = header.Status; // هذا سيقوم بتحديث حالة الأزرار تلقائياً
                Notes = header.Notes ?? "";
                SelectedDepartment = Departments.FirstOrDefault(d => d.DepartmentId == header.DepartmentId);

                Lines.Clear();
                foreach (var d in details) Lines.Add(d);

                StatusMessage = "تم التحميل.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في التحميل: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AddLineAsync()
        {
            // فحص 1: هل الزر يعمل أصلاً؟
            StatusMessage = "تم ضغط الزر... جاري التحقق";

            // فحص 2: هل المادة مختارة؟
            if (SelectedItem == null)
            {
                StatusMessage = "خطأ: لم يتم اختيار المادة من القائمة (SelectedItem is null).";
                return;
            }

            // فحص 3: هل الكمية رقم صحيح؟
            if (string.IsNullOrWhiteSpace(QtyText) || !decimal.TryParse(QtyText, out var qty) || qty <= 0)
            {
                StatusMessage = "خطأ: الكمية غير صحيحة أو فارغة.";
                return;
            }

            IsBusy = true;
            try
            {
                // فحص 4: التحقق من التوثيق
                if (_session.CurrentUser == null)
                {
                    StatusMessage = "خطأ: المستخدم غير مسجل دخول.";
                    return;
                }

                StatusMessage = "جاري الإضافة لقاعدة البيانات...";

                await _service.AddOrUpdateLineAsync(_requisitionId, SelectedItem.Id, qty, LineNotes, _session.CurrentUser.UserId);

                // تحديث الجدول
                await InitAsync();

                // تنظيف الحقول
                SelectedItem = null;
                QtyText = "1";
                LineNotes = "";
                StatusMessage = "تمت إضافة السطر بنجاح.";
            }
            catch (Exception ex)
            {
                StatusMessage = "حدث خطأ أثناء الحفظ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RemoveLineAsync(long detailId)
        {
            IsBusy = true;
            try
            {
                await EnsureAuth();
                await _service.RemoveLineAsync(detailId, _session.CurrentUser!.UserId);
                await InitAsync();
                StatusMessage = "تم الحذف.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task SubmitAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureAuth();
                await _service.SubmitAsync(_requisitionId, _session.CurrentUser!.UserId);
                await InitAsync(); // لتحديث الحالة إلى Submitted
                StatusMessage = "تم إرسال الطلب بنجاح.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task ApproveAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureAuth();
                await _service.ApproveAsync(_requisitionId, _session.CurrentUser!.UserId);
                await InitAsync();
                StatusMessage = "تم اعتماد الطلب.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task RejectAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureAuth();
                await _service.RejectAsync(_requisitionId, _session.CurrentUser!.UserId);
                await InitAsync();
                StatusMessage = "تم رفض الطلب.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task CancelAsync()
        {
            IsBusy = true;
            try
            {
                await EnsureAuth();
                await _service.CancelAsync(_requisitionId, _session.CurrentUser!.UserId);
                await InitAsync();
                StatusMessage = "تم إلغاء الطلب.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task FulfillLineAsync()
        {
            if (SelectedLine == null) return;
            IsBusy = true;
            try
            {
                await EnsureAuth();
                var remaining = SelectedLine.RemainingQty;
                if (remaining <= 0) throw new InvalidOperationException("السطر مكتمل.");

                var trxId = await _service.FulfillLineAsync(
                    SelectedLine.RequisitionDetailId,
                    remaining,
                    _session.CurrentUser!.UserId,
                    SelectedDepartment?.DepartmentId,
                    "صرف آلي"
                );

                await InitAsync();
                StatusMessage = $"تم الصرف. رقم الحركة: {trxId}";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task EnsureAuth()
        {
            if (!_session.IsAuthenticated || _session.CurrentUser == null)
                throw new InvalidOperationException("يجب تسجيل الدخول.");
            await Task.CompletedTask;
        }
    }
}