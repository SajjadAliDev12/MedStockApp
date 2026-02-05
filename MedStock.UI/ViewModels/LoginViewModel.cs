using System;
using System.Threading.Tasks;
using MedStock.Services.Interfaces;

namespace MedStock.UI.ViewModels
{
    public sealed class LoginViewModel : ViewModelBase
    {
        private readonly IUserService _users;
        private readonly ISessionContext _session;

        private string _username = "";
        private string _errorMessage = "";
        private bool _isBusy;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        // Password will be provided by the view (PasswordBox) via command parameter
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    LoginCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand<string> LoginCommand { get; }

        public LoginViewModel(IUserService users, ISessionContext session)
        {
            _users = users ?? throw new ArgumentNullException(nameof(users));
            _session = session ?? throw new ArgumentNullException(nameof(session));

            LoginCommand = new RelayCommand<string>(async pwd => await LoginAsync(pwd),
                pwd => !IsBusy && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrEmpty(pwd));
        }

        private async Task LoginAsync(string? password)
        {
            ErrorMessage = "";
            IsBusy = true;

            try
            {
                var user = await _users.AuthenticateAsync(Username.Trim(), password ?? "");
                _session.SetUser(user);
            }
            catch (Exception ex)
            {
                // English: display friendly message (already safe and intended)
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
