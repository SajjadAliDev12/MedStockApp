using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class CheckRowVm : ViewModelBase
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
        public bool IsActive { get; init; }

        private bool _isChecked;
        public bool IsChecked { get => _isChecked; set => SetProperty(ref _isChecked, value); }
    }

    public sealed class ItemCategoriesViewModel : ViewModelBase
    {
        private readonly IItemCategoriesService _svc;
        private readonly ISessionContext _session;

        private string _itemSearch = "";
        private string _status = "";
        private bool _isBusy;

        public ObservableCollection<IdNameRow> Items { get; } = new();
        public ObservableCollection<CheckRowVm> Categories { get; } = new();

        private IdNameRow? _selectedItem;
        public IdNameRow? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    ApplyCommand.RaiseCanExecuteChanged();
                    _ = LoadAssignedAsync();
                }
            }
        }


        public string ItemSearchText { get => _itemSearch; set => SetProperty(ref _itemSearch, value); }
        public string StatusMessage { get => _status; private set => SetProperty(ref _status, value); }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RefreshItemsCommand.RaiseCanExecuteChanged();
                    ApplyCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public RelayCommand RefreshItemsCommand { get; }
        public RelayCommand ApplyCommand { get; }

        public ItemCategoriesViewModel(IItemCategoriesService svc, ISessionContext session)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            RefreshItemsCommand = new RelayCommand(async () => await RefreshItemsAsync(), () => !IsBusy);
            ApplyCommand = new RelayCommand(async () => await ApplyAsync(), () => !IsBusy && SelectedItem != null);
        }

        public async Task InitAsync()
        {
            await RefreshItemsAsync();
            await LoadCategoriesAsync();
        }

        private async Task RefreshItemsAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            try
            {
                Items.Clear();
                var data = await _svc.GetItemsAsync(ItemSearchText);
                foreach (var r in data) Items.Add(r);
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

        private async Task LoadCategoriesAsync()
        {
            IsBusy = true;
            StatusMessage = "";
            try
            {
                Categories.Clear();
                var data = await _svc.GetCategoriesAsync();
                foreach (var c in data)
                {
                    Categories.Add(new CheckRowVm { Id = c.Id, Name = c.Name, IsActive = c.IsActive, IsChecked = false });
                }
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

        private async Task LoadAssignedAsync()
        {
            if (SelectedItem == null) return;

            IsBusy = true;
            StatusMessage = "";
            try
            {
                var assigned = await _svc.GetAssignedCategoryIdsAsync(SelectedItem.Id);

                foreach (var c in Categories)
                    c.IsChecked = assigned.Contains(c.Id);
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

        private async Task ApplyAsync()
        {
            if (SelectedItem == null) return;

            IsBusy = true;
            StatusMessage = "";
            try
            {
                if (!_session.IsAuthenticated || _session.CurrentUser == null)
                    throw new InvalidOperationException("غير مسجل دخول.");

                var ids = Categories.Where(x => x.IsChecked).Select(x => x.Id).ToList();
                await _svc.SetAssignedCategoriesAsync(SelectedItem.Id, ids, _session.CurrentUser.UserId);

                StatusMessage = "تم تطبيق التصنيفات على المادة.";
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
