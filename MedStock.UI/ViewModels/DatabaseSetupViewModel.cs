using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace MedStock.UI.ViewModels
{
    public sealed class DatabaseSetupViewModel : ViewModelBase
    {
        private readonly ConfigFilePointer _configPointer;

        private string _server = ".";
        private string _database = "MedStockDb";
        private string _activityType = "Hospital";

        // حقول المصادقة الجديدة
        private bool _useSqlAuth;
        private string _username = "";
        private string _password = "";

        private string _statusMessage = "";
        private bool _isBusy;

        public DatabaseSetupViewModel(ConfigFilePointer configPointer)
        {
            _configPointer = configPointer;
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
            TestCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsBusy);

            LoadCurrentConfig();
        }

        public string Server { get => _server; set => SetProperty(ref _server, value); }
        public string Database { get => _database; set => SetProperty(ref _database, value); }
        public string ActivityType { get => _activityType; set => SetProperty(ref _activityType, value); }

        // الخصائص الجديدة للتحكم في المصادقة
        public bool UseSqlAuth
        {
            get => _useSqlAuth;
            set
            {
                if (SetProperty(ref _useSqlAuth, value))
                {
                    OnPropertyChanged(nameof(IsSqlAuthVisible)); // لتفعيل/تعطيل الحقول في الواجهة
                }
            }
        }

        public bool IsSqlAuthVisible => UseSqlAuth; // خاصية مساعدة للـ Binding

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) SaveCommand.RaiseCanExecuteChanged(); } }

        public RelayCommand SaveCommand { get; }
        public RelayCommand TestCommand { get; }

        private void LoadCurrentConfig()
        {
            try
            {
                if (!File.Exists(_configPointer.Path)) return;
                var json = File.ReadAllText(_configPointer.Path);
                var node = JsonNode.Parse(json);

                var cs = node?["ConnectionStrings"]?["HospitalDb"]?.ToString();
                if (!string.IsNullOrEmpty(cs))
                {
                    var builder = new SqlConnectionStringBuilder(cs);
                    Server = builder.DataSource;
                    Database = builder.InitialCatalog;

                    // استكشاف نوع المصادقة من نص الاتصال
                    if (!builder.IntegratedSecurity)
                    {
                        UseSqlAuth = true;
                        Username = builder.UserID;
                        Password = builder.Password;
                    }
                    else
                    {
                        UseSqlAuth = false;
                    }
                }

                ActivityType = node?["AppSettings"]?["ActivityType"]?.ToString() ?? "Hospital";
            }
            catch { /* تجاهل */ }
        }

        private async Task TestConnectionAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري اختبار الاتصال...";
            try
            {
                var cs = BuildConnectionString();
                using var conn = new SqlConnection(cs);
                await conn.OpenAsync();
                StatusMessage = "تم الاتصال بنجاح!";
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل الاتصال: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        private async Task SaveAsync()
        {
            IsBusy = true;
            try
            {
                var cs = BuildConnectionString();

                var json = await File.ReadAllTextAsync(_configPointer.Path);
                var root = JsonNode.Parse(json)!.AsObject();

                if (root["ConnectionStrings"] == null) root["ConnectionStrings"] = new JsonObject();
                root["ConnectionStrings"]!["HospitalDb"] = cs;

                if (root["AppSettings"] == null) root["AppSettings"] = new JsonObject();
                root["AppSettings"]!["ActivityType"] = ActivityType;

                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_configPointer.Path, root.ToJsonString(options));

                StatusMessage = "تم حفظ الإعدادات بنجاح. يرجى إعادة تشغيل البرنامج.";
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في الحفظ: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = Database,
                TrustServerCertificate = true,
                ConnectTimeout = 30
            };

            if (UseSqlAuth)
            {
                builder.IntegratedSecurity = false;
                builder.UserID = Username;
                builder.Password = Password;
            }
            else
            {
                builder.IntegratedSecurity = true; // Windows Auth
            }

            return builder.ConnectionString;
        }
    }
}