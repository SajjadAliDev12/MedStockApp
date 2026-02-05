using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MedStock.UI.ViewModels
{
    public sealed class ExpiryReportViewModel : ViewModelBase
    {
        private readonly IReportsService _service;

        // المتغيرات
        private int _daysThreshold = 180; // الافتراضي: 6 أشهر
        private bool _isBusy;
        private string _statusMessage = "";

        public ObservableCollection<ExpiryReportRow> Rows { get; } = new();

        public ExpiryReportViewModel(IReportsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
        }

        // الخصائص
        public int DaysThreshold
        {
            get => _daysThreshold;
            set
            {
                if (SetProperty(ref _daysThreshold, value))
                {
                    // إعادة التحميل تلقائياً عند تغيير المدة
                    _ = LoadDataAsync();
                }
            }
        }

        public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public RelayCommand RefreshCommand { get; }

        public async Task LoadDataAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            Rows.Clear();
            try
            {
                var data = await _service.GetExpiryReportAsync(DaysThreshold);
                foreach (var item in data) Rows.Add(item);

                StatusMessage = $"تم العثور على {data.Count} مادة.";
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