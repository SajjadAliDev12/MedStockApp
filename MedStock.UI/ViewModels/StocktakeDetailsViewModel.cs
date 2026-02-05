using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    // كلاس مساعد للواجهة (لتفعيل التعديل وحساب الفرق لحظياً)
    public class StocktakeLineVm : ViewModelBase
    {
        public long DetailId { get; init; }
        public string ItemName { get; init; } = "";
        public string Unit { get; init; } = "";
        public decimal SystemQty { get; init; }

        private string _physicalText = "";
        public string PhysicalText
        {
            get => _physicalText;
            set
            {
                if (SetProperty(ref _physicalText, value))
                {
                    OnPropertyChanged(nameof(Difference));
                    OnPropertyChanged(nameof(DiffColor));
                }
            }
        }

        public decimal Difference
        {
            get
            {
                if (decimal.TryParse(_physicalText, out var p)) return p - SystemQty;
                return 0; // أو -SystemQty إذا اعتبرنا الفراغ صفراً
            }
        }

        public string DiffColor => Difference < 0 ? "Crimson" : (Difference > 0 ? "Green" : "Black");
    }

    public sealed class StocktakeDetailsViewModel : ViewModelBase
    {
        private readonly IStocktakeService _service;
        private readonly ISessionContext _session;
        private readonly INavigationService _nav;

        public ObservableCollection<StocktakeLineVm> Lines { get; } = new();

        private bool _isBusy;
        private string _status = "";
        private int _currentId;

        public StocktakeDetailsViewModel(IStocktakeService service, ISessionContext session, INavigationService nav)
        {
            _service = service;
            _session = session;
            _nav = nav;

            SaveCommand = new RelayCommand(async () => await SaveAsync());
            PostCommand = new RelayCommand(async () => await PostAsync());
            BackCommand = new RelayCommand(() => _nav.NavigateTo<StocktakesViewModel>());
        }

        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public string StatusMessage { get => _status; private set => SetProperty(ref _status, value); }

        public RelayCommand SaveCommand { get; }
        public RelayCommand PostCommand { get; }
        public RelayCommand BackCommand { get; }

        public async Task InitAsync()
        {
            // نأخذ الـ ID من المتغير الستاتيك (أو يمكن استخدام Context Service كما فعلت سابقاً)
            _currentId = StocktakesViewModel.SelectedStocktakeIdForDetail;
            if (_currentId == 0) return;

            IsBusy = true;
            try
            {
                var data = await _service.GetDetailsAsync(_currentId);
                Lines.Clear();
                foreach (var row in data)
                {
                    Lines.Add(new StocktakeLineVm
                    {
                        DetailId = row.DetailId,
                        ItemName = row.ItemName,
                        Unit = row.Unit,
                        SystemQty = row.SystemQty,
                        PhysicalText = row.PhysicalQty?.ToString("0.##") ?? ""
                    });
                }
                StatusMessage = $"تم تحميل {Lines.Count} صنف للجرد.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري الحفظ...";
            try
            {
                var counts = new Dictionary<long, decimal>();
                foreach (var line in Lines)
                {
                    if (decimal.TryParse(line.PhysicalText, out var val))
                    {
                        counts[line.DetailId] = val;
                    }
                }

                await _service.SaveCountsAsync(_currentId, counts);
                StatusMessage = "تم حفظ الأرقام (مسودة).";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task PostAsync()
        {
            // حفظ أولاً
            await SaveAsync();

            IsBusy = true;
            StatusMessage = "جاري الترحيل والتسوية...";
            try
            {
                if (!_session.IsAuthenticated) throw new InvalidOperationException("غير مصرح.");

                await _service.PostAsync(_currentId, _session.CurrentUser!.UserId);

                StatusMessage = "تم ترحيل الجرد وتعديل المخزون بنجاح!";
                // يمكن العودة للقائمة
                _nav.NavigateTo<StocktakesViewModel>(); 
            }
            catch (Exception ex) { StatusMessage = "فشل الترحيل: " + ex.Message; }
            finally { IsBusy = false; }
        }
    }
}