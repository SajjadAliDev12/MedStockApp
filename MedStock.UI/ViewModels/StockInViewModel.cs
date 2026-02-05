using MedStock.Data.Entities;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MedStock.UI.ViewModels
{
    // كلاس مساعد لتمثيل سطر الإدخال في الواجهة (للفصل عن الـ DTO)
    public sealed class StockInLineVm : ViewModelBase
    {
        private string _itemIdText = "";
        private string _itemName = ""; // للعرض فقط في الجدول
        private string _batchCode = "";
        private string _receivedDateText = "";
        private string _expiryDateText = "";
        private string _qtyText = "";
        private string _unitCostText = "";
        private string _locationCode = "";
        public string LocationCode { get => _locationCode; set => SetProperty(ref _locationCode, value); }

        public string ItemIdText { get => _itemIdText; set => SetProperty(ref _itemIdText, value); }
        public string ItemName { get => _itemName; set => SetProperty(ref _itemName, value); }
        public string BatchCode { get => _batchCode; set => SetProperty(ref _batchCode, value); }
        public string ReceivedDateText { get => _receivedDateText; set => SetProperty(ref _receivedDateText, value); }
        public string ExpiryDateText { get => _expiryDateText; set => SetProperty(ref _expiryDateText, value); }
        public string QtyText { get => _qtyText; set => SetProperty(ref _qtyText, value); }
        public string UnitCostText { get => _unitCostText; set => SetProperty(ref _unitCostText, value); }
    }

    public sealed class StockInViewModel : ViewModelBase
    {
        private readonly IInventoryService _inventory;
        private readonly IItemsService _items;
        private readonly ISuppliersService _suppliers; // خدمة الموردين الجديدة
        private readonly ISessionContext _session;

        // حالة الشاشة
        private string _statusMessage = "";
        private bool _isBusy;

        // حقول رأس الفاتورة
        private string _departmentIdText = "";
        private string _notes = "";
        private IdNameRow? _selectedSupplier; // المورد المختار

        // حقول إضافة سطر جديد (Editor)
        private IdNameRow? _selectedItem; // المادة المختارة من القائمة
        private StockInLineVm _newLine = new();

        // القوائم (Lookups & Grid)
        public ObservableCollection<IdNameRow> ItemsLookup { get; } = new();
        public ObservableCollection<IdNameRow> SuppliersLookup { get; } = new();
        public ObservableCollection<StockInLineVm> Lines { get; } = new();

        public StockInViewModel(
            IInventoryService inventory,
            IItemsService items,
            ISuppliersService suppliers,
            ISessionContext session)
        {
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _suppliers = suppliers ?? throw new ArgumentNullException(nameof(suppliers));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            // تعريف الأوامر
            AddLineCommand = new RelayCommand(AddLine);
            RemoveLineCommand = new RelayCommand<StockInLineVm>(RemoveLine);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy && Lines.Any());

            // تهيئة القيم الافتراضية للسطر الجديد
            ResetNewLine();
        }

        // --- خصائص الربط (Binding Properties) ---

        public string DepartmentIdText { get => _departmentIdText; set => SetProperty(ref _departmentIdText, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    SaveCommand.RaiseCanExecuteChanged();
            }
        }

        public IdNameRow? SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public IdNameRow? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    // عند اختيار مادة، نقوم بتحديث الـ ID في السطر الجديد تلقائياً
                    if (_selectedItem != null)
                    {
                        NewLine.ItemIdText = _selectedItem.Id.ToString();
                        NewLine.ItemName = _selectedItem.Name;
                    }
                }
            }
        }

        public StockInLineVm NewLine
        {
            get => _newLine;
            set => SetProperty(ref _newLine, value);
        }

        public RelayCommand AddLineCommand { get; }
        public RelayCommand<StockInLineVm> RemoveLineCommand { get; }
        public RelayCommand SaveCommand { get; }

        // --- الوظائف (Methods) ---

        // دالة يتم استدعاؤها من الـ View عند التحميل (Loaded Event)
        public async Task InitAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            try
            {
                // 1. تحميل قائمة المواد النشطة
                var items = await _items.GetItemsAsync(null);
                ItemsLookup.Clear();
                foreach (var i in items.Where(x => x.IsActive))
                {
                    ItemsLookup.Add(new IdNameRow { Id = i.ItemId, Name = i.Name, IsActive = true });
                }

                // 2. تحميل قائمة الموردين النشطين
                var suppliers = await _suppliers.GetLookupAsync();
                SuppliersLookup.Clear();
                foreach (var s in suppliers)
                {
                    SuppliersLookup.Add(s);
                }

                StatusMessage = "جاهز.";
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل التحميل: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void AddLine()
        {
            // التحقق من صحة المدخلات قبل الإضافة للجدول
            if (SelectedItem == null)
            {
                StatusMessage = "يجب اختيار المادة أولاً.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewLine.BatchCode))
            {
                StatusMessage = "رقم التشغيلة (Batch) مطلوب.";
                return;
            }

            if (!decimal.TryParse(NewLine.QtyText, out var qty) || qty <= 0)
            {
                StatusMessage = "الكمية يجب أن تكون رقماً أكبر من صفر.";
                return;
            }

            if (!decimal.TryParse(NewLine.UnitCostText, out var cost) || cost < 0)
            {
                StatusMessage = "السعر يجب أن يكون رقماً صحيحاً (غير سالب).";
                return;
            }

            // التحقق من صحة التواريخ (إذا أدخلت)
            if (!string.IsNullOrWhiteSpace(NewLine.ExpiryDateText) && !DateOnly.TryParse(NewLine.ExpiryDateText, out _))
            {
                StatusMessage = "تاريخ الانتهاء غير صحيح (yyyy-MM-dd).";
                return;
            }

            // إضافة السطر للقائمة
            Lines.Add(new StockInLineVm
            {
                ItemIdText = SelectedItem.Id.ToString(),
                ItemName = SelectedItem.Name,
                BatchCode = NewLine.BatchCode,
                ReceivedDateText = NewLine.ReceivedDateText,
                ExpiryDateText = NewLine.ExpiryDateText,
                QtyText = qty.ToString("0.##"),
                UnitCostText = cost.ToString("0:C0"),
                LocationCode = NewLine.LocationCode.Trim(),
            });

            // إعادة تهيئة حقول الإدخال لسطر جديد
            ResetNewLine();
            StatusMessage = "";
            SaveCommand.RaiseCanExecuteChanged();
        }

        private void RemoveLine(StockInLineVm? line)
        {
            if (line != null && Lines.Contains(line))
            {
                Lines.Remove(line);
                SaveCommand.RaiseCanExecuteChanged();
            }
        }

        private void ResetNewLine()
        {
            SelectedItem = null; // تفريغ اختيار المادة
            NewLine = new StockInLineVm
            {
                ReceivedDateText = DateTime.Today.ToString("yyyy-MM-dd"),
                QtyText = "1",
                UnitCostText = "0",
                BatchCode = "",
                LocationCode = ""
            };
        }

        private async Task SaveAsync()
        {
            if (!Lines.Any()) return;

            IsBusy = true;
            StatusMessage = "جاري الحفظ...";

            try
            {
                // التحقق من تسجيل الدخول
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("يجب تسجيل الدخول أولاً.");

                // تحويل بيانات الرأس
                int? deptId = null;
                if (int.TryParse(DepartmentIdText, out var d) && d > 0) deptId = d;

                var req = new StockInRequest
                {
                    CreatedByUserId = _session.CurrentUser.UserId,
                    DepartmentId = deptId,
                    SupplierId = SelectedSupplier?.Id, // استخدام المورد المختار
                    ReasonCode = TransactionReasons.Purchase,
                    Notes = Notes
                };

                // تحويل بيانات الأسطر
                foreach (var line in Lines)
                {
                    var itemId = int.Parse(line.ItemIdText);
                    var qty = decimal.Parse(line.QtyText);
                    var cost = decimal.Parse(line.UnitCostText);

                    DateOnly? received = null;
                    if (DateOnly.TryParse(line.ReceivedDateText, out var r)) received = r;

                    DateOnly? expiry = null;
                    if (DateOnly.TryParse(line.ExpiryDateText, out var e)) expiry = e;

                    req.Lines.Add(new StockInLine
                    {
                        ItemId = itemId,
                        BatchCode = line.BatchCode,
                        Quantity = qty,
                        UnitCost = cost,
                        ReceivedDate = received,
                        ExpiryDate = expiry,
                        LocationCode = string.IsNullOrWhiteSpace(line.LocationCode) ? null : line.LocationCode.Trim(),

                        // ملاحظة: يمكن إضافة SupplierRef هنا لو أردنا رقم فاتورة لكل سطر
                    });
                }

                // استدعاء الخدمة للحفظ في قاعدة البيانات
                var trxId = await _inventory.StockInAsync(req);

                StatusMessage = $"تم الحفظ بنجاح. رقم الحركة: {trxId}";

                // تنظيف الشاشة بعد الحفظ الناجح
                Lines.Clear();
                Notes = "";
                SelectedSupplier = null;
                ResetNewLine();
                SaveCommand.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = "حدث خطأ: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}