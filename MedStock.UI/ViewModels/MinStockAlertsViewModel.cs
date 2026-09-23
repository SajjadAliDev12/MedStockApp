using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.Win32;

namespace MedStock.UI.ViewModels
{
    public sealed class MinStockAlertsViewModel : ViewModelBase
    {
        private readonly IAlertsService _svc;

        private string _search = "";
        private string _status = "";
        private bool _isBusy;

        public ObservableCollection<MinStockAlertRow> Rows { get; } = new();

        public string SearchText { get => _search; set => SetProperty(ref _search, value); }
        public string StatusMessage { get => _status; private set => SetProperty(ref _status, value); }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    ExportCsvCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public MinStockAlertsViewModel(IAlertsService svc)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            ExportCsvCommand = new RelayCommand(async () => await ExportCsvAsync(), () => !IsBusy && Rows.Count > 0);
        }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            try
            {
                Rows.Clear();
                var data = await _svc.GetMinStockAlertsAsync(SearchText);
                foreach (var r in data) Rows.Add(r);

                StatusMessage = Rows.Count == 0 ? "لا توجد تنبيهات حالياً." : $"عدد التنبيهات: {Rows.Count}";
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

        private async Task ExportCsvAsync()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("ItemId,Name,Sku,MinStock,CurrentStock");

                foreach (var row in Rows)
                {
                    var name = (row.Name ?? "").Replace(",", " ");
                    var sku = (row.Sku ?? "").Replace(",", " ");
                    sb.AppendLine($"{row.ItemId},{name},{sku},{row.MinStock},{row.CurrentStock}");
                }

                var dlg = new SaveFileDialog
                {
                    FileName = $"MinStockAlerts_{DateTime.Now:yyyyMMdd}",
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
