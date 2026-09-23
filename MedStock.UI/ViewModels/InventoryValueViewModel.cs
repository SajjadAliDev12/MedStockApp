using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.Win32;

namespace MedStock.UI.ViewModels
{
    public sealed class InventoryValueViewModel : ViewModelBase
    {
        private readonly IReportsService _reports;

        private string _searchText = "";
        private bool _isBusy;
        private string _statusMessage = "";

        public ObservableCollection<InventoryValueRow> Rows { get; } = new();

        public InventoryValueViewModel(IReportsService reports)
        {
            _reports = reports ?? throw new ArgumentNullException(nameof(reports));

            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            ExportCsvCommand = new RelayCommand(async () => await ExportCsvAsync(), () => !IsBusy && Rows.Count > 0);
        }

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
                    ExportCsvCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public decimal GrandTotal => Rows.Sum(r => r.TotalValue);

        public RelayCommand RefreshCommand { get; }
        public RelayCommand ExportCsvCommand { get; }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري تحميل البيانات...";
            try
            {
                Rows.Clear();
                var data = await _reports.GetInventoryValueAsync(SearchText);
                foreach (var r in data) Rows.Add(r);

                OnPropertyChanged(nameof(GrandTotal));
                StatusMessage = Rows.Count == 0 ? "لا توجد مواد مطابقة." : $"عدد المواد: {Rows.Count}";
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
                sb.AppendLine("ItemId,ItemName,Sku,Unit,Qty,AvgCost,TotalValue");

                foreach (var row in Rows)
                {
                    var name = (row.ItemName ?? "").Replace(",", " ");
                    var sku = (row.Sku ?? "").Replace(",", " ");
                    var unit = (row.Unit ?? "").Replace(",", " ");
                    sb.AppendLine($"{row.ItemId},{name},{sku},{unit},{row.Qty},{row.AvgCost},{row.TotalValue}");
                }

                var dlg = new SaveFileDialog
                {
                    FileName = $"InventoryValue_{DateTime.Now:yyyyMMdd}",
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
