using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

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
                    RefreshCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand RefreshCommand { get; }

        public MinStockAlertsViewModel(IAlertsService svc)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
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
    }
}
