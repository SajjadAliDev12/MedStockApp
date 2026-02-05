using MedStock.Data.Context;
using MedStock.Services.Implementations;
using MedStock.Services.Interfaces;
using MedStock.UI.ViewModels;
using MedStock.UI.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Threading; // ضروري للـ Thread
using System.Windows;
using System.Windows.Markup; // ضروري لضبط لغة الـ WPF Binding
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace MedStock.UI
{
    public partial class App : Application
    {
        private string _configPath = string.Empty;
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. إعداد مسار ملف الإعدادات في AppData
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MedStockPro");

            if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);

            _configPath = Path.Combine(appDataPath, "appsettings.json");

            // 2. ضمان وجود الملف
            if (!File.Exists(_configPath))
            {
                var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (File.Exists(defaultPath))
                    File.Copy(defaultPath, _configPath);
                else
                    File.WriteAllText(_configPath, "{ \"ConnectionStrings\": { \"HospitalDb\": \"\" }, \"AppSettings\": { \"ActivityType\": \"Hospital\" } }");
            }

            base.OnStartup(e);

            // 3. ضبط اللغة (System + WPF Binding) - تم التعديل هنا
            var arCulture = new CultureInfo("ar-IQ");

            // ضبط لغة الكود الخلفي (C#)
            CultureInfo.DefaultThreadCurrentCulture = arCulture;
            CultureInfo.DefaultThreadCurrentUICulture = arCulture;
            Thread.CurrentThread.CurrentCulture = arCulture;
            Thread.CurrentThread.CurrentUICulture = arCulture;

            // [هام جداً] ضبط لغة الواجهة (XAML Bindings) لتظهر العملة د.ع
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(arCulture.IetfLanguageTag)));

            // 4. بناء الـ Host وتجهيز الخدمات
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.SetBasePath(Directory.GetCurrentDirectory());
                    cfg.AddJsonFile(_configPath, optional: false, reloadOnChange: true);
                })
                .ConfigureServices((ctx, services) =>
                {
                    var cs = ctx.Configuration.GetConnectionString("HospitalDb");

                    services.AddDbContext<HospitalInventoryDbContext>(opt =>
                    {
                        if (!string.IsNullOrEmpty(cs))
                        {
                            opt.UseSqlServer(cs, sql =>
                            {
                                sql.EnableRetryOnFailure(5);
                                sql.CommandTimeout(30);
                            });
                        }
                        opt.EnableDetailedErrors();
                    });

                    // تسجيل الخدمات الأساسية
                    services.AddSingleton<Data.Context.IDbContextFactory<HospitalInventoryDbContext>, HospitalDbContextFactory>();
                    services.AddSingleton<DbExecutor>();
                    services.AddSingleton<ISessionContext, SessionContext>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<IUserService, UserService>();
                    services.AddSingleton<IInventoryService, InventoryService>();
                    services.AddSingleton<IItemsService, ItemsService>();
                    services.AddSingleton<ICategoriesService, CategoriesService>();
                    services.AddSingleton<IItemCategoriesService, ItemCategoriesService>();
                    services.AddSingleton<IAlertsService, AlertsService>();
                    services.AddSingleton<ISuppliersService, SuppliersService>();
                    services.AddSingleton<IUsersManagementService, UsersManagementService>();
                    services.AddSingleton<IReportsService, ReportsService>();
                    services.AddSingleton<IStocktakeService, StocktakeService>();
                    services.AddSingleton<IRequisitionsService, RequisitionsService>();
                    services.AddSingleton<IDepartmentsService, DepartmentsService>();
                    services.AddSingleton<IAuditService, AuditService>();

                    // تسجيل الـ ViewModels
                    services.AddSingleton<MainWindowViewModel>();
                    services.AddSingleton<LoginViewModel>();
                    services.AddSingleton<DashboardViewModel>();
                    services.AddSingleton<ItemsViewModel>();
                    services.AddSingleton<StockInViewModel>();
                    services.AddSingleton<StockOutViewModel>();
                    services.AddSingleton<CategoriesViewModel>();
                    services.AddSingleton<ItemCategoriesViewModel>();
                    services.AddSingleton<MinStockAlertsViewModel>();
                    services.AddSingleton<SuppliersViewModel>();
                    services.AddSingleton<UsersViewModel>();
                    services.AddSingleton<StockCardViewModel>();
                    services.AddSingleton<StocktakesViewModel>();
                    services.AddSingleton<StocktakeDetailsViewModel>();
                    services.AddSingleton<RequisitionsViewModel>();
                    services.AddSingleton<RequisitionDetailsViewModel>();
                    services.AddSingleton<ILanguageService, LanguageService>();
                    services.AddSingleton<ExpiryReportViewModel>();
                    services.AddSingleton<AuditLogsViewModel>();
                    services.AddSingleton<DepartmentsViewModel>();
                    services.AddSingleton<ConsumptionReportViewModel>();
                    services.AddSingleton<DatabaseSetupViewModel>();

                    // تسجيل الـ Views
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<DatabaseSetupView>();
                    services.AddSingleton<AuditLogsView>();
                    services.AddSingleton<ConsumptionReportView>();

                    // تسجيل المؤشر لملف الإعدادات (مرة واحدة فقط)
                    services.AddSingleton(new ConfigFilePointer { Path = _configPath });

                    // سياق البيانات المشترك
                    services.AddSingleton<RequisitionContext>();
                })
                .Build();

            // 5. منطق التشغيل الذكي
            CheckDatabaseAndRun();
        }

        private async void CheckDatabaseAndRun()
        {
            var config = _host.Services.GetRequiredService<IConfiguration>();
            var cs = config.GetConnectionString("HospitalDb");

            bool canConnect = false;
            if (!string.IsNullOrEmpty(cs))
            {
                try
                {
                    var dbFactory = _host.Services.GetRequiredService<Data.Context.IDbContextFactory<HospitalInventoryDbContext>>();
                    using var db = dbFactory.CreateDbContext();
                    canConnect = await db.Database.CanConnectAsync();
                }
                catch { canConnect = false; }
            }

            if (!canConnect)
            {
                var setupView = _host.Services.GetRequiredService<DatabaseSetupView>();
                var setupVm = _host.Services.GetRequiredService<DatabaseSetupViewModel>();
                setupView.DataContext = setupVm;
                setupView.Show();
            }
            else
            {
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host != null)
                await _host.StopAsync();

            _host?.Dispose();
            base.OnExit(e);
        }
    }

    public class ConfigFilePointer { public string Path { get; set; } = ""; }
}