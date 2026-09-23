using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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
            ExportCsvCommand = new RelayCommand(async () => await ExportCsvAsync(), () => !IsBusy && Rows.Count > 0);
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

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    ExportCsvCommand.RaiseCanExecuteChanged();
            }
        }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

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

        private async Task ExportCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("ItemName,BatchNo,ExpiryDate,DaysRemaining,Qty,Status");

                foreach (var row in Rows)
                {
                    var name = (row.ItemName ?? "").Replace(",", " ");
                    var batch = (row.BatchNo ?? "").Replace(",", " ");
                    var status = (row.Status ?? "").Replace(",", " ");
                    var expiry = row.ExpiryDate?.ToString("yyyy/MM/dd") ?? "";
                    sb.AppendLine($"{name},{batch},{expiry},{row.DaysRemaining},{row.Qty},{status}");
                }

                var dlg = new SaveFileDialog
                {
                    FileName = $"ExpiryReport_{DateTime.Now:yyyyMMdd}",
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