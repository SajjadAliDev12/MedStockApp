using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    // كلاس مساعد لتمثيل الدور في الواجهة (للاختيار)
    public sealed class RoleCheckVm : ViewModelBase
    {
        public int RoleId { get; init; }
        public string RoleName { get; init; } = "";

        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    }

    public sealed class UsersViewModel : ViewModelBase
    {
        private readonly IUsersManagementService _service;
        private readonly ISessionContext _session;

        private string _search = "";
        private bool _isBusy;
        private string _statusMessage = "";

        // Editor Fields
        private int? _editId;
        private string _username = "";
        private string _displayName = "";
        private bool _isActive = true;

        private UserListRow? _selected;

        public ObservableCollection<UserListRow> Rows { get; } = new();
        public ObservableCollection<RoleCheckVm> AllRoles { get; } = new();

        public UsersViewModel(IUsersManagementService service, ISessionContext session)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            RefreshCommand = new RelayCommand(async () => await RefreshAsync());
            NewCommand = new RelayCommand(StartNew);
            SaveCommand = new RelayCommand(async p => await SaveAsync(p as string), _ => !IsBusy);
            ToggleActiveCommand = new RelayCommand(async () => await ToggleActiveAsync(), () => Selected != null);
            ResetPassCommand = new RelayCommand(async p => await ResetPassAsync(p as string), _ => Selected != null);
        }

        // Bindings
        public string SearchText { get => _search; set => SetProperty(ref _search, value); }
        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) SaveCommand.RaiseCanExecuteChanged(); } }

        public string UsernameText { get => _username; set => SetProperty(ref _username, value); }
        public string DisplayNameText { get => _displayName; set => SetProperty(ref _displayName, value); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        // هل هذا مستخدم جديد؟ (لإظهار حقل الباسورد)
        public bool IsNewUser => _editId == null;

        public UserListRow? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                {
                    ToggleActiveCommand.RaiseCanExecuteChanged();
                    ResetPassCommand.RaiseCanExecuteChanged();
                    if (_selected != null) LoadForEdit(_selected);
                    else StartNew();
                }
            }
        }

        public RelayCommand RefreshCommand { get; }
        public RelayCommand NewCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand ToggleActiveCommand { get; }
        public RelayCommand ResetPassCommand { get; }

        public async Task RefreshAsync()
        {
            bool bootstrap = EnsureAdminOrBootstrap(out bool denied);
            if (denied) return;

            IsBusy = true;
            StatusMessage = "جاري التحميل...";
            try
            {
                // 1. Load Roles first if empty
                if (!AllRoles.Any())
                {
                    var roles = await _service.GetRolesAsync();
                    foreach (var r in roles) AllRoles.Add(new RoleCheckVm { RoleId = r.Id, RoleName = r.Name });
                }

                // 2. Load Users
                var list = await _service.GetListAsync(SearchText);
                Rows.Clear();
                foreach (var u in list) Rows.Add(u);

                StatusMessage = bootstrap
                    ? "تحذير: المستخدم الحالي بلا أدوار (وضع التمهيد) — يُرجى إسناد دور Admin."
                    : "";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        private void StartNew()
        {
            _selected = null;
            OnPropertyChanged(nameof(Selected));

            _editId = null;
            UsernameText = "";
            DisplayNameText = "";
            IsActive = true;

            // Uncheck all roles
            foreach (var r in AllRoles) r.IsSelected = false;

            OnPropertyChanged(nameof(IsNewUser));
            StatusMessage = "مستخدم جديد";
        }

        private void LoadForEdit(UserListRow user)
        {
            _editId = user.UserId;
            UsernameText = user.Username;
            DisplayNameText = user.DisplayName;
            IsActive = user.IsActive;

            // Set Roles
            var userRoles = user.Roles.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var r in AllRoles)
            {
                r.IsSelected = userRoles.Contains(r.RoleName);
            }

            OnPropertyChanged(nameof(IsNewUser));
            StatusMessage = "تعديل المستخدم";
        }

        /// <summary>
        /// Bootstrap rule: legacy users have no roles assigned. A role-less
        /// current user gets full access (with warning); otherwise Admin role required.
        /// Returns true when in bootstrap mode. Sets denied=true and StatusMessage when blocked.
        /// </summary>
        private bool EnsureAdminOrBootstrap(out bool denied)
        {
            denied = false;
            if (!_session.IsAuthenticated || _session.CurrentUser == null)
            {
                StatusMessage = "غير مصرح.";
                denied = true;
                return false;
            }

            var roles = _session.CurrentUser.Roles;
            if (roles == null || roles.Count == 0)
            {
                StatusMessage = "تحذير: المستخدم الحالي بلا أدوار (وضع التمهيد) — يُرجى إسناد دور Admin.";
                return true;
            }

            if (!_session.CurrentUser.IsInRole("Admin"))
            {
                StatusMessage = "غير مصرح: تتطلب صلاحية Admin.";
                denied = true;
                return false;
            }

            return false;
        }

        private async Task SaveAsync(string? passwordParam)
        {
            EnsureAdminOrBootstrap(out bool denied);
            if (denied) return;

            IsBusy = true;
            try
            {
                // Validate
                var selectedRoles = AllRoles.Where(r => r.IsSelected).Select(r => r.RoleId).ToList();
                if (!selectedRoles.Any()) throw new InvalidOperationException("يجب اختيار دور واحد على الأقل.");

                var req = new UserUpsertRequest
                {
                    UserId = _editId,
                    Username = UsernameText,
                    DisplayName = DisplayNameText,
                    Password = _editId == null ? passwordParam : null, // PasswordBox value passed as command parameter, never bound
                    IsActive = IsActive,
                    RoleIds = selectedRoles
                };

                await _service.SaveAsync(req, _session.CurrentUser!.UserId);

                StatusMessage = "تم الحفظ بنجاح.";
                await RefreshAsync();
                StartNew();
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        private async Task ToggleActiveAsync()
        {
            if (Selected == null) return;
            EnsureAdminOrBootstrap(out bool denied);
            if (denied) return;
            try
            {
                await _service.ToggleActiveAsync(Selected.UserId, _session.CurrentUser!.UserId);
                await RefreshAsync();
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }

        private async Task ResetPassAsync(string? passwordParam)
        {
            if (Selected == null) return;
            EnsureAdminOrBootstrap(out bool denied);
            if (denied) return;
            if (string.IsNullOrWhiteSpace(passwordParam))
            {
                StatusMessage = "أدخل كلمة المرور الجديدة في حقل كلمة المرور ثم اضغط الزر.";
                return;
            }

            try
            {
                await _service.ResetPasswordAsync(Selected.UserId, passwordParam, _session.CurrentUser!.UserId);
                StatusMessage = $"تم تغيير كلمة مرور {Selected.Username} بنجاح.";
            }
            catch (Exception ex) { StatusMessage = ex.Message; }
        }
    }
}