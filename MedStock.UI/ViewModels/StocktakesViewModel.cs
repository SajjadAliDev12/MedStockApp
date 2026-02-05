using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;


namespace MedStock.UI.ViewModels
{
    public sealed class StocktakesViewModel : ViewModelBase
    {
        private readonly IStocktakeService _service;
        private readonly ISessionContext _session;
        private readonly INavigationService _nav;

        // الوسيط لنقل البيانات (مقبول جداً في MVP)
        public static int SelectedStocktakeIdForDetail;

        public ObservableCollection<StocktakeListRow> Rows { get; } = new();

        private bool _isBusy;
        private string _status = "";
        private StocktakeListRow? _selectedRow;

        public StocktakesViewModel(IStocktakeService service, ISessionContext session, INavigationService nav)
        {
            _service = service;
            _session = session;
            _nav = nav;

            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            CreateCommand = new RelayCommand(async () => await CreateAsync());

            // التعديل هنا: الأمر يقبل Parameter ليسمح بالضغط المباشر من الزر داخل السطر
            OpenDetailsCommand = new RelayCommand<object>(OpenDetails);
        }

        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
        public string StatusMessage { get => _status; private set => SetProperty(ref _status, value); }

        public StocktakeListRow? SelectedRow
        {
            get => _selectedRow;
            set => SetProperty(ref _selectedRow, value);
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand CreateCommand { get; }
        public RelayCommand<object> OpenDetailsCommand { get; } // لاحظ التغيير هنا

        public async Task InitAsync() => await RefreshAsync();

        private async Task RefreshAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _service.GetListAsync();
                Rows.Clear();
                foreach (var item in list) Rows.Add(item);
                StatusMessage = $"تم تحميل {Rows.Count} سجل.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        private async Task CreateAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري إنشاء الجرد...";
            try
            {
                if (!_session.IsAuthenticated) throw new InvalidOperationException("يرجى تسجيل الدخول.");

                // إنشاء المسودة
                var id = await _service.CreateDraftAsync(_session.CurrentUser!.UserId, null);

                // تحديث القائمة
                await RefreshAsync();

                // الانتقال فوراً للتفاصيل
                SelectedStocktakeIdForDetail = id;
                _nav.NavigateTo<StocktakeDetailsViewModel>();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
            finally { IsBusy = false; }
        }

        // تم تحسين هذه الدالة لتقبل المتغير من الزر أو تأخذه من السطر المختار
        private void OpenDetails(object? parameter)
        {
            int idToOpen = 0;

            if (parameter is StocktakeListRow row)
            {
                idToOpen = row.StocktakeId;
            }
            else if (SelectedRow != null)
            {
                idToOpen = SelectedRow.StocktakeId;
            }

            if (idToOpen > 0)
            {
                SelectedStocktakeIdForDetail = idToOpen;
                _nav.NavigateTo<StocktakeDetailsViewModel>();
            }
        }
    }
}