using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class DepartmentsViewModel : ViewModelBase
    {
        private readonly IDepartmentsService _svc;
        private readonly ISessionContext _session;

        private string _search = "";
        private string _code = "";
        private string _name = "";
        private string _notes = "";
        private bool _isActive = true;
        private int? _editId;

        private string _status = "";
        private bool _isBusy;

        public ObservableCollection<DepartmentListRow> Rows { get; } = new();
        private DepartmentListRow? _selected;

        public string SearchText { get => _search; set => SetProperty(ref _search, value); }
        public string CodeText { get => _code; set => SetProperty(ref _code, value); }
        public string NameText { get => _name; set => SetProperty(ref _name, value); }
        public string NotesText { get => _notes; set => SetProperty(ref _notes, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }
        public string StatusMessage { get => _status; private set => SetProperty(ref _status, value); }

        public DepartmentListRow? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    EditCommand.RaiseCanExecuteChanged();
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }

        public DepartmentsViewModel(IDepartmentsService svc, ISessionContext session)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsBusy);
            NewCommand = new RelayCommand(StartNew, () => !IsBusy);
            EditCommand = new RelayCommand(StartEdit, () => !IsBusy && Selected != null);
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
            ToggleActiveCommand = new RelayCommand(async () => await ToggleActiveAsync(), () => !IsBusy && Selected != null);

            StartNew();
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    NewCommand.RaiseCanExecuteChanged();
                    EditCommand.RaiseCanExecuteChanged();
                    SaveCommand.RaiseCanExecuteChanged();
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public async Task RefreshAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            try
            {
                Rows.Clear();
                var data = await _svc.GetAsync(SearchText);
                foreach (var r in data) Rows.Add(r);
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

        private void StartNew()
        {
            _editId = null;
            CodeText = "";
            NameText = "";
            NotesText = "";
            IsActive = true;
        }

        private void StartEdit()
        {
            if (Selected == null) return;
            _editId = Selected.DepartmentId;
            CodeText = Selected.Code ?? "";
            NameText = Selected.Name;
            NotesText = Selected.Notes ?? "";
            IsActive = Selected.IsActive;
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                var id = await _svc.SaveAsync(new DepartmentUpsertRequest
                {
                    DepartmentId = _editId,
                    Code = CodeText,
                    Name = NameText,
                    Notes = NotesText,
                    IsActive = IsActive
                }, _session.CurrentUser.UserId);

                await RefreshAsync();
                StatusMessage = _editId.HasValue ? $"تم التعديل. رقم القسم: {id}" : $"تمت الإضافة. رقم القسم: {id}";
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
        public async Task InitAsync()
        {
            await RefreshAsync();
        }
        private async Task ToggleActiveAsync()
        {
            if (Selected == null) return;

            IsBusy = true;
            StatusMessage = "";
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                await _svc.SetActiveAsync(Selected.DepartmentId, !Selected.IsActive, _session.CurrentUser.UserId);
                await RefreshAsync();
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
