using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.Win32; // من أجل SaveFileDialog

namespace MedStock.UI.ViewModels
{
    public sealed class ConsumptionReportViewModel : ViewModelBase
    {
        private readonly IReportsService _reportsService;
        private readonly ICategoriesService _categoriesService;
        private readonly INavigationService _nav;

        // الفلاتر
        private DateTime? _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); // أول الشهر
        private DateTime? _toDate = DateTime.Today;
        private CategoryListRow? _selectedCategory;

        // الحالة
        private bool _isBusy;
        private string _statusMessage = "";

        // البيانات
        private ConsumptionSummaryRow? _selectedSummary;

        public ObservableCollection<CategoryListRow> Categories { get; } = new();
        public ObservableCollection<ConsumptionSummaryRow> SummaryRows { get; } = new();
        public ObservableCollection<ConsumptionDetailRow> DetailRows { get; } = new();

        public ConsumptionReportViewModel(
            IReportsService reportsService,
            ICategoriesService categoriesService,
            INavigationService nav)
        {
            _reportsService = reportsService;
            _categoriesService = categoriesService;
            _nav = nav;

            SearchCommand = new RelayCommand(async () => await SearchAsync(), () => !IsBusy);
            ExportCommand = new RelayCommand(async () => await ExportToCsvAsync(), () => !IsBusy && SummaryRows.Count > 0);
        }

        // --- Properties ---
        public DateTime? FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime? ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

        public CategoryListRow? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ConsumptionSummaryRow? SelectedSummary
        {
            get => _selectedSummary;
            set
            {
                if (SetProperty(ref _selectedSummary, value))
                {
                    _ = LoadDetailsAsync(); // تحميل التفاصيل عند اختيار سطر
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    SearchCommand.RaiseCanExecuteChanged();
                    ExportCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        // إحصائيات سريعة
        public string SummaryTotalInfo => $"عدد المواد: {SummaryRows.Count} | إجمالي الكميات: {SummaryRows.Sum(x => x.TotalQty):N0}";

        // --- Commands ---
        public RelayCommand SearchCommand { get; }
        public RelayCommand ExportCommand { get; }

        // --- Methods ---

        public async Task InitAsync()
        {
            if (Categories.Count == 0)
            {
                try
                {
                    IsBusy = true;
                    // إضافة خيار "الكل" يدوياً
                    Categories.Add(new CategoryListRow { CategoryId = 0, Name = "--- كل التصنيفات ---" });

                    // التصحيح هنا: نمرر null لجلب كل التصنيفات
                    var cats = await _categoriesService.GetAsync(null);

                    foreach (var c in cats) Categories.Add(c);

                    SelectedCategory = Categories.FirstOrDefault();
                }
                catch (Exception ex) { StatusMessage = "فشل تحميل التصنيفات: " + ex.Message; }
                finally { IsBusy = false; }
            }
        }

        private async Task SearchAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري جلب البيانات...";
            SummaryRows.Clear();
            DetailRows.Clear();

            try
            {
                int? catId = (SelectedCategory != null && SelectedCategory.CategoryId > 0)
                    ? SelectedCategory.CategoryId
                    : null;

                var result = await _reportsService.GetConsumptionSummaryAsync(FromDate, ToDate, catId, null);

                foreach (var item in result) SummaryRows.Add(item);

                StatusMessage = $"تم العثور على {SummaryRows.Count} مادة تم صرفها.";
                OnPropertyChanged(nameof(SummaryTotalInfo));
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في التقرير: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadDetailsAsync()
        {
            if (SelectedSummary == null) return;

            IsBusy = true;
            try
            {
                var details = await _reportsService.GetConsumptionDetailsAsync(SelectedSummary.ItemId, FromDate, ToDate);
                DetailRows.Clear();
                foreach (var d in details) DetailRows.Add(d);
            }
            catch { /* تجاهل أخطاء التفاصيل الفرعية */ }
            finally { IsBusy = false; }
        }

        private async Task ExportToCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                // العناوين
                sb.AppendLine("Code,Item Name,Unit,Total Quantity,Transactions Count");

                foreach (var row in SummaryRows)
                {
                    // تنظيف النصوص من الفواصل لتجنب تكسر الـ CSV
                    var name = row.ItemName.Replace(",", " ");
                    sb.AppendLine($"{row.Sku},{name},{row.UnitName},{row.TotalQty},{row.TransactionCount}");
                }

                // فتح نافذة الحفظ
                var dlg = new SaveFileDialog
                {
                    FileName = $"ConsumptionReport_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv",
                    Filter = "CSV Files (*.csv)|*.csv"
                };

                if (dlg.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dlg.FileName, sb.ToString(), Encoding.UTF8);
                    StatusMessage = "تم التصدير بنجاح!";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل التصدير: " + ex.Message;
            }
        }
    }
}