using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class StockCardViewModel : ViewModelBase
    {
        private readonly IReportsService _reports;
        private readonly IItemsService _items;

        // الفلاتر
        private IdNameRow? _selectedItem;
        private DateTime _fromDate = DateTime.Today.AddDays(-30); // افتراضياً آخر شهر
        private DateTime _toDate = DateTime.Today;

        private bool _isBusy;
        private string _statusMessage = "";

        // القوائم
        public ObservableCollection<IdNameRow> ItemsLookup { get; } = new();
        public ObservableCollection<StockCardRow> ReportRows { get; } = new();

        public StockCardViewModel(IReportsService reports, IItemsService items)
        {
            _reports = reports ?? throw new ArgumentNullException(nameof(reports));
            _items = items ?? throw new ArgumentNullException(nameof(items));

            GenerateCommand = new RelayCommand(async () => await GenerateAsync(), () => !IsBusy);
        }

        // Bindings
        public DateTime FromDate { get => _fromDate; set => SetProperty(ref _fromDate, value); }
        public DateTime ToDate { get => _toDate; set => SetProperty(ref _toDate, value); }

        public IdNameRow? SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { if (SetProperty(ref _isBusy, value)) GenerateCommand.RaiseCanExecuteChanged(); }
        }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public RelayCommand GenerateCommand { get; }

        // يتم استدعاؤها عند فتح الشاشة
        public async Task InitAsync()
        {
            IsBusy = true;
            try
            {
                var items = await _items.GetItemsAsync(null);
                ItemsLookup.Clear();
                foreach (var i in items)
                {
                    ItemsLookup.Add(new IdNameRow { Id = i.ItemId, Name = i.Name, IsActive = i.IsActive });
                }
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task GenerateAsync()
        {
            if (SelectedItem == null)
            {
                StatusMessage = "الرجاء اختيار مادة لعرض تقريرها.";
                return;
            }

            IsBusy = true;
            StatusMessage = "جاري إنشاء التقرير...";
            ReportRows.Clear();

            try
            {
                // ضبط نهاية اليوم ليشمل حركات اليوم الأخير كاملاً
                var endOfToDate = ToDate.Date.AddDays(1).AddTicks(-1);

                var data = await _reports.GetStockCardAsync(SelectedItem.Id, FromDate.Date, endOfToDate);

                foreach (var row in data) ReportRows.Add(row);

                StatusMessage = $"تم العثور على {ReportRows.Count - 1} حركة.";
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