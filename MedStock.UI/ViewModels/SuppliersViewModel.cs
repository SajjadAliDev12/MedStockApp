using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class SuppliersViewModel : ViewModelBase
    {
        private readonly ISuppliersService _service;
        private readonly ISessionContext _session;

        // حالة العرض
        private string _search = "";
        private bool _isBusy;
        private string _statusMessage = "";

        // حقول التعديل (Editor Fields)
        private int? _editId;
        private string _name = "";
        private string _phone = "";
        private string _email = "";
        private string _address = "";
        private bool _isActive = true;

        private SupplierListRow? _selected;

        public ObservableCollection<SupplierListRow> Rows { get; } = new();

        public SuppliersViewModel(ISuppliersService service, ISessionContext session)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new RelayCommand(StartNew);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
            ToggleActiveCommand = new RelayCommand(async () => await ToggleActiveAsync(), () => Selected != null && !IsBusy);
        }

        // Properties Binding
        public string SearchText { get => _search; set => SetProperty(ref _search, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        // Editor Binding Properties
        public string NameText { get => _name; set => SetProperty(ref _name, value); }
        public string PhoneText { get => _phone; set => SetProperty(ref _phone, value); }
        public string EmailText { get => _email; set => SetProperty(ref _email, value); }
        public string AddressText { get => _address; set => SetProperty(ref _address, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        public SupplierListRow? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                    if (_selected != null)
                    {
                        // تعبئة الحقول عند الاختيار
                        _editId = _selected.SupplierId;
                        NameText = _selected.Name;
                        PhoneText = _selected.Phone ?? "";
                        EmailText = _selected.Email ?? "";
                        // ملاحظة: العنوان غير موجود في الـ ListRow لتخفيف الحمل، يمكن جلبه إذا أردت أو الاكتفاء بالتعديل بدونه
                        // للتبسيط هنا سنفترض أن المستخدم سيدخل العنوان إذا أراد تعديله أو نتركه فارغاً
                        AddressText = "";
                        IsActive = _selected.IsActive;
                        StatusMessage = "تم اختيار المورد للتعديل.";
                    }
                    else
                    {
                        StartNew();
                    }
                }
            }
        }
        public async Task InitAsync()
        {
            await RefreshAsync();
        }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري التحميل...";
            try
            {
                var list = await _service.GetListAsync(SearchText);
                Rows.Clear();
                foreach (var item in list) Rows.Add(item);
                StatusMessage = $"تم تحميل {Rows.Count} مورد.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void StartNew()
        {
            _selected = null;
            OnPropertyChanged(nameof(Selected)); // لإلغاء تحديد الجدول بصرياً

            _editId = null;
            NameText = "";
            PhoneText = "";
            EmailText = "";
            AddressText = "";
            IsActive = true;
            StatusMessage = "وضع الإضافة الجديد.";
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(NameText))
            {
                StatusMessage = "اسم المورد مطلوب.";
                return;
            }

            IsBusy = true;
            StatusMessage = "جاري الحفظ...";

            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                var req = new SupplierUpsertRequest
                {
                    SupplierId = _editId,
                    Name = NameText.Trim(),
                    Phone = string.IsNullOrWhiteSpace(PhoneText) ? null : PhoneText.Trim(),
                    Email = string.IsNullOrWhiteSpace(EmailText) ? null : EmailText.Trim(),
                    Address = string.IsNullOrWhiteSpace(AddressText) ? null : AddressText.Trim(),
                    IsActive = IsActive
                };

                var id = await _service.SaveAsync(req, _session.CurrentUser.UserId);

                await RefreshAsync();
                StartNew();
                StatusMessage = _editId.HasValue ? "تم التعديل بنجاح." : $"تمت إضافة المورد رقم {id}.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في الحفظ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (Selected == null) return;

            IsBusy = true;
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                await _service.ToggleActiveAsync(Selected.SupplierId, _session.CurrentUser.UserId);
                await RefreshAsync();
                StatusMessage = "تم تغيير حالة التفعيل.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}