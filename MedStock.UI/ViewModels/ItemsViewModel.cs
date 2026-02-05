using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows; // ضروري لعمل MessageBox

namespace MedStock.UI.ViewModels
{
    public sealed class ItemsViewModel : ViewModelBase
    {
        private readonly IItemsService _items;
        private readonly ISessionContext _session;

        private string _searchText = "";
        private string _statusMessage = "";
        private bool _isBusy;

        private ItemListRow? _selected;

        // Editor fields
        private int? _editId;
        private string _name = "";
        private string _sku = "";
        private string _unitOfMeasure = "";
        private string _reorderLevelText = "0";
        private string _minStockText = "";
        private string _description = "";
        private bool _isActive = true;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private int _totalRecords = 0;
        private readonly int _pageSize = 50;
        public ObservableCollection<ItemListRow> Rows { get; } = new();
        public ObservableCollection<ItemBatchRow> Batches { get; } = new();
        public int CurrentPage
        {
            get => _currentPage;
            set { if (SetProperty(ref _currentPage, value)) UpdatePaginationCommands(); }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { if (SetProperty(ref _totalPages, value)) UpdatePaginationCommands(); }
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        public string PageInfo => $"صفحة {CurrentPage} من {TotalPages} (العدد: {TotalRecords})";
        public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    NewCommand.RaiseCanExecuteChanged();
                    EditCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public ItemListRow? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    // تفعيل الأزرار عند الاختيار
                    EditCommand.RaiseCanExecuteChanged();
                    DeleteCommand.RaiseCanExecuteChanged();
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();

                    if (_selected != null)
                    {
                        _ = LoadBatchesAsync(_selected.ItemId);
                    }
                    else
                    {
                        StartNew();
                        Batches.Clear();
                    }
                }
            }
        }

        private async Task LoadBatchesAsync(int itemId)
        {
            try
            {
                Batches.Clear();
                var data = await _items.GetBatchesAsync(itemId);
                foreach (var row in data) Batches.Add(row);
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل تحميل الأرصدة: " + ex.Message;
            }
        }

        // خصائص النصوص
        public string NameText { get => _name; set => SetProperty(ref _name, value); }
        public string SkuText { get => _sku; set => SetProperty(ref _sku, value); }
        public string UnitOfMeasureText { get => _unitOfMeasure; set => SetProperty(ref _unitOfMeasure, value); }
        public string ReorderLevelText { get => _reorderLevelText; set => SetProperty(ref _reorderLevelText, value); }
        public string MinStockText { get => _minStockText; set => SetProperty(ref _minStockText, value); }
        public string DescriptionText { get => _description; set => SetProperty(ref _description, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        public bool IsEditing => _editId.HasValue;

        // الأوامر
        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand NextPageCommand { get; }
        public RelayCommand PrevPageCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }
        public RelayCommand DeleteCommand { get; }

        // البناء Constructor
        // تم حذف IDialogService من المعاملات
        public ItemsViewModel(IItemsService items, ISessionContext session)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new RelayCommand(StartNew, () => !IsBusy);
            NextPageCommand = new RelayCommand(async () => await ChangePage(1), () => CurrentPage < TotalPages);
            PrevPageCommand = new RelayCommand(async () => await ChangePage(-1), () => CurrentPage > 1);
            EditCommand = new RelayCommand(async () => await StartEditAsync(), () => !IsBusy && Selected != null);

            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);

            ToggleActiveCommand = new RelayCommand(async () => await ToggleActiveAsync(), () => !IsBusy && Selected != null);

            DeleteCommand = new RelayCommand(async () => await DeleteAsync(), () => !IsBusy && Selected != null);

            StartNew();
        }

        public async Task RefreshAsync(bool resetPage = false) // عدلها لتقبل بارامتر
        {
            if (resetPage) CurrentPage = 1;

            IsBusy = true;
            try
            {
                var filter = new ItemFilter
                {
                    SearchText = SearchText,
                    IsActive = null, // أو حسب حالة CheckBox عندك
                    PageNumber = CurrentPage,
                    PageSize = _pageSize
                };

                var result = await _items.SearchAsync(filter);

                Rows.Clear();
                foreach (var item in result.Items) Rows.Add(item);

                // تحديث العدادات
                TotalRecords = result.TotalCount;
                TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;

                OnPropertyChanged(nameof(PageInfo));
                UpdatePaginationCommands();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task ChangePage(int offset)
        {
            CurrentPage += offset;
            await RefreshAsync(false);
        }

        private void UpdatePaginationCommands()
        {
            NextPageCommand.RaiseCanExecuteChanged();
            PrevPageCommand.RaiseCanExecuteChanged();
        }

        private void StartNew()
        {
            _editId = null;
            NameText = "";
            SkuText = "";
            UnitOfMeasureText = "";
            ReorderLevelText = "0";
            MinStockText = "";
            DescriptionText = "";
            IsActive = true;
            OnPropertyChanged(nameof(IsEditing));
        }

        private async Task StartEditAsync()
        {
            if (Selected == null) return;

            IsBusy = true;
            StatusMessage = "";
            try
            {
                var dto = await _items.GetForEditAsync(Selected.ItemId);
                _editId = dto.ItemId;
                NameText = dto.Name;
                SkuText = dto.Sku;
                UnitOfMeasureText = dto.UnitOfMeasure;
                ReorderLevelText = dto.ReorderLevel.ToString("0.##",CultureInfo.InvariantCulture);
                MinStockText = dto.MinStock?.ToString("0.##",CultureInfo.InvariantCulture) ?? "";
                DescriptionText = dto.Description ?? "";
                IsActive = dto.IsActive;
                OnPropertyChanged(nameof(IsEditing));
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private static bool TryParseDecimalFlexible(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
                   || decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private async Task SaveAsync()
        {
            StatusMessage = "";
            IsBusy = true;
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser is null)
                {
                    StatusMessage = "انتهت الجلسة، يرجى تسجيل الدخول.";
                    return;
                }

                var name = (NameText ?? "").Trim();
                var sku = (SkuText ?? "").Trim();
                var uom = (UnitOfMeasureText ?? "").Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    StatusMessage = "اسم المادة مطلوب.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(sku))
                {
                    StatusMessage = "SKU مطلوب.";
                    return;
                }
                if (string.IsNullOrWhiteSpace(uom))
                {
                    StatusMessage = "وحدة القياس مطلوبة.";
                    return;
                }

                if (!TryParseDecimalFlexible((ReorderLevelText ?? "").Trim(), out var reorder) || reorder < 0m)
                {
                    StatusMessage = "حد إعادة الطلب غير صحيح.";
                    return;
                }

                decimal? minStock = null;
                if (!string.IsNullOrWhiteSpace(MinStockText))
                {
                    if (!TryParseDecimalFlexible(MinStockText.Trim(), out var ms) || ms < 0m)
                    {
                        StatusMessage = "حد التنبيه غير صحيح.";
                        return;
                    }
                    minStock = ms;
                }

                var req = new ItemUpsertRequest
                {
                    ItemId = _editId,
                    Name = name,
                    Sku = sku,
                    UnitOfMeasure = uom,
                    ReorderLevel = reorder,
                    MinStock = minStock,
                    Description = string.IsNullOrWhiteSpace(DescriptionText) ? null : DescriptionText.Trim(),
                    IsActive = IsActive
                };

                var id = await _items.SaveAsync(req, _session.CurrentUser.UserId);

                var msg = _editId.HasValue ? "تم التعديل بنجاح." : "تم الإضافة بنجاح.";
                StatusMessage = msg;

                await RefreshAsync();
                StartNew();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        private async Task ToggleActiveAsync()
        {
            if (Selected == null) return;
            IsBusy = true;
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser is null) return;

                var newState = !Selected.IsActive;
                await _items.SetActiveAsync(Selected.ItemId, newState, _session.CurrentUser.UserId);
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message ;
            }
            finally { IsBusy = false; }
        }

        private async Task DeleteAsync()
        {
            if (Selected == null) return;

            // تأكيد الحذف باستخدام MessageBox
            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف المادة '{Selected.Name}' نهائياً؟\nتنبيه: لا يمكن حذف مادة إذا كان لها أرصدة أو حركات سابقة.",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsBusy = true;
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser is null) return;

                await _items.DeleteAsync(Selected.ItemId, _session.CurrentUser.UserId);

                StatusMessage = "تم حذف المادة بنجاح." ;
                await RefreshAsync();
                StartNew();
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل الحذف: " + ex.Message;
            }
            finally { IsBusy = false; }
        }
        public async Task InitAsync()
        {
            // دائماً قم بالتحديث عند الدخول لرؤية التغييرات (إضافة/حذف)
            await RefreshAsync();
        }
    }
}