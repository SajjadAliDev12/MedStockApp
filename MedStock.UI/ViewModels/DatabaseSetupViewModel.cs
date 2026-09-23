using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using MedStock.Services.Implementations;
using MedStock.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MedStock.UI.ViewModels
{
    public sealed class DatabaseSetupViewModel : ViewModelBase
    {
        private readonly ConfigFilePointer _configPointer;
        private readonly IEmailService _emailService;

        private string _server = ".";
        private string _database = "MedStockDb";
        private string _activityType = "Hospital";

        // حقول المصادقة الجديدة
        private bool _useSqlAuth;
        private string _username = "";
        private string _password = "";

        // حقول البريد
        private string _adminEmail = "";
        private string _testResult = "";

        private string _statusMessage = "";
        private bool _isBusy;

        public DatabaseSetupViewModel(ConfigFilePointer configPointer, IConfiguration config)
        {
            _configPointer = configPointer;
            // لا يتطلب تسجيل DI: نبني الخدمة من IConfiguration المسجل تلقائياً في الـ Host
            _emailService = EmailService.FromConfiguration(config ?? throw new ArgumentNullException(nameof(config)));
            SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
            TestCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsBusy);
            TestEmailCommand = new RelayCommand(async () => await TestEmailAsync(), () => !IsBusy);
            BackupCommand = new RelayCommand(async () => await BackupAsync(), () => !IsBusy);

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

        public string AdminEmail { get => _adminEmail; set => SetProperty(ref _adminEmail, value); }
        public string TestResult { get => _testResult; private set => SetProperty(ref _testResult, value); }

        public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }
        public bool IsBusy { get => _isBusy; set { if (SetProperty(ref _isBusy, value)) { SaveCommand.RaiseCanExecuteChanged(); TestCommand.RaiseCanExecuteChanged(); TestEmailCommand.RaiseCanExecuteChanged(); BackupCommand.RaiseCanExecuteChanged(); } } }

        public RelayCommand SaveCommand { get; }
        public RelayCommand TestCommand { get; }
        public RelayCommand TestEmailCommand { get; }
        public RelayCommand BackupCommand { get; }

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

                AdminEmail = node?["Email"]?["AdminEmail"]?.ToString() ?? "";
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

                JsonObject root = null;

                // 1. محاولة قراءة الملف الحالي إذا وجد
                if (File.Exists(_configPointer.Path))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(_configPointer.Path);
                        // التحقق مما إذا كان الملف يحتوي json صالح
                        root = JsonNode.Parse(json)?.AsObject();
                    }
                    catch
                    {
                        // تجاهل الخطأ في حالة كان الملف تالفاً أو فارغاً
                    }
                }

                // 2. إذا لم ينجح التحميل (غير موجود أو تالف)، ننشئ كائناً جديداً
                if (root == null) root = new JsonObject();

                // 3. ضمان وجود الأقسام (Sections) قبل الكتابة
                if (root["ConnectionStrings"] == null) root["ConnectionStrings"] = new JsonObject();
                root["ConnectionStrings"]["HospitalDb"] = cs;

                if (root["AppSettings"] == null) root["AppSettings"] = new JsonObject();
                root["AppSettings"]["ActivityType"] = ActivityType;

                // 3b. حفظ بريد المشرف مع الحفاظ على بقية مفاتيح Email (ApiKey...)
                if (root["Email"] == null) root["Email"] = new JsonObject();
                root["Email"]["AdminEmail"] = AdminEmail;

                // 4. التأكد من وجود المجلد قبل الكتابة (لتجنب DirectoryNotFoundException)
                var directory = Path.GetDirectoryName(_configPointer.Path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

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
        private async Task TestEmailAsync()
        {
            IsBusy = true;
            try
            {
                if (!_emailService.IsConfigured)
                {
                    TestResult = "تم التخطي: مفتاح البريد (ApiKey) غير مُعد في الإعدادات.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(AdminEmail))
                {
                    TestResult = "أدخل بريد المشرف أولاً.";
                    return;
                }

                TestResult = "جاري الإرسال...";
                var ok = await _emailService.SendAsync(AdminEmail.Trim(), "بريد تجريبي — MedStock", "تم إعداد البريد بنجاح.");
                TestResult = ok ? "تم إرسال البريد التجريبي بنجاح." : "فشل إرسال البريد التجريبي.";
            }
            catch (Exception ex)
            {
                TestResult = "خطأ: " + ex.Message;
            }
            finally { IsBusy = false; }
        }

        private async Task BackupAsync()
        {
            IsBusy = true;
            StatusMessage = "جاري النسخ الاحتياطي...";
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MedStockPro", "Backups");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, $"{Database}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

                // النسخ الاحتياطي يجب أن يعمل ضد master، فنبدل InitialCatalog
                var masterBuilder = new SqlConnectionStringBuilder(BuildConnectionString()) { InitialCatalog = "master" };
                using var conn = new SqlConnection(masterBuilder.ConnectionString);
                await conn.OpenAsync();
                using var cmd = new SqlCommand($"BACKUP DATABASE [{Database}] TO DISK='{file}' WITH INIT", conn) { CommandTimeout = 300 };
                await cmd.ExecuteNonQueryAsync();

                StatusMessage = "تم النسخ الاحتياطي: " + file;
            }
            catch (Exception ex)
            {
                StatusMessage = "فشل النسخ الاحتياطي: " + ex.Message;
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