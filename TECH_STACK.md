# TECH_STACK — MedStock (مخزون مثبت من الملفات، فحص 2026-09)

> البروتوكول الزمني: `Get-Date → 2026-09`، `dotnet --version → 9.0.318`. كل الإصدارات أدناه مقروءة من `*.csproj` — لا تخمين. تجنب `Deprecated`: لا حزم مهجورة مرصودة، لكن `SendGrid` و`Azure.Core` و`Newtonsoft.Json` تستحق مراجعة (أدناه).

## 1. Runtime / Language / Target

| البند | القيمة المثبتة | المصدر |
|-------|----------------|--------|
| SDK المثبت | `9.0.318` | `dotnet --version` |
| Target Data | `net8.0`, `Nullable enable`, `ImplicitUsings enable` | `MedStock.Data.csproj` |
| Target Services | `net8.0-windows`, `UseWPF true`, `Nullable+ImplicitUsings` | `MedStock.Services.csproj` |
| Target UI | `WinExe`, `net8.0-windows`, `UseWPF true`, `Nullable+ImplicitUsings` | `MedStock.UI.csproj` |
| Solution | VS `17.14.36717.8`, Format `12.00`, configs `Debug\|AnyCPU / Release\|AnyCPU` | `MedStock.sln` |
| اللغة/الثقافة | C# (latest ضمن net8)، ثقافة قسرية `ar-IQ` + `FrameworkElement.LanguageProperty.OverrideMetadata(ar-IQ)` + `FlowDirection=RTL` | `App.xaml.cs:50-62`, `App.xaml:16-18` |
| OS النشر | `win-x64` | `FolderProfile.pubxml` |

## 2. Core Packages (NuGet) ودور كل منها

| الحزمة | الإصدار | أين | الدور المثبت |
|--------|---------|-----|--------------|
| `Microsoft.EntityFrameworkCore` | `8.0.23` | Data | ORM (Queries + ChangeTracker + Auditing) |
| `Microsoft.EntityFrameworkCore.SqlServer` | `8.0.23` | Data + UI + Services | Provider (`UseSqlServer`, `EnableRetryOnFailure(5)`, `CommandTimeout(30)`) |
| `Microsoft.EntityFrameworkCore.Tools` | `8.0.23` (`PrivateAssets=all`) | Data | Scaffold/Migrations CLI (لا Migrations مستخدمة فعلياً) |
| `Microsoft.EntityFrameworkCore.Design` | `8.0.23` (`PrivateAssets=all`) | UI + Services | Design-time support |
| `Microsoft.Extensions.Hosting` | `8.0.1` | UI | `Host.CreateDefaultBuilder` في `App.OnStartup` |
| `Microsoft.Extensions.DependencyInjection` | `10.0.2` | UI | DI (`AddSingleton` لكل شيء) — **ملاحظة**: v10 مع `net8` يعمل لكنه أحدث من `Hosting 8.0.1`؛ توحيد مقترح (انظر ERRORS) |
| `Microsoft.Extensions.Configuration.Json` | `8.0.1` | UI | `AddJsonFile(_configPath)` من `%AppData%` |
| `Newtonsoft.Json` | `13.0.4` | Services | مُعلن لكن لا استخدام مرصود في `Implementations` (الـ Audit يستخدم `System.Text.Json`) — مرشح للإزالة |
| `SendGrid` | `9.29.3` | Services | مُعلن بلا استدعاء مرصود |
| `SendGrid.Extensions.DependencyInjection` | `1.0.1` | UI + Services | مُعلن بلا `AddSendGrid` مرصود في `App.xaml.cs` — ميت/غير مكتمل |
| `Microsoft.Data.SqlClient` (transitive) | عبر EF(SqlServer) + `using` مباشر | UI | `DatabaseSetupViewModel` (`SqlConnectionStringBuilder` + `SqlConnection.Open` للاختبار) |
| `Azure.Core` | transitive (لا مرجع مباشر) | Services | `using Azure.Core;` يتيم في `RequisitionsService.cs:1` — يُحذف |

## 3. Database / Tooling / External

- **DB**: SQL Server — `appsettings.json: Server=.\SQLEXPRESS; Database=HospitalInventoryDb; Trusted_Connection=True; TrustServerCertificate=True`. DB خارج الريبو؛ Triggers الأربعة أسماء فقط في `OnModelCreating` (لا SQL).
- **Local dev**: `DatabaseSetupView` (اختبار/حفظ cs + `ActivityType`) → `%AppData%\MedStockPro\appsettings.json`. لا `docker-compose`, لا `Dockerfile`, لا `.env`.
- **External services**: `SendGrid` (معطل/غير موصول). لا Application Insights/Serilog/Seq.
- **Publish**: `Properties/PublishProfiles/FolderProfile.pubxml`: `FileSystem → bin\Release\net8.0-windows\publish\MedStockApp`, `SelfContained=true`, `SingleFile=true`, `ReadyToRun=true`, `TargetFramework net8.0-windows`, `Runtime win-x64`.

## 4. Commands Reference (مختبرة/مستنتجة من البنية)

```powershell
# التاريخ/البيئة (البروتوكول الأول)
Get-Date -Format "yyyy-MM"   # -> 2026-09
dotnet --version              # -> 9.0.318

# بناء (مُختبر 2026-09-23: 0 Errors)
dotnet build MedStock.sln

# اختبارات (مُختبرة: 23/23 خضراء — 22 وحدة + 1 تكامل تتخطى ذاتياً بلا SQL)
dotnet test MedStock.sln

# تشغيل (WPF — يتطلب Windows + SQL Server reachable)
dotnet run --project MedStock.UI
```

## 5. ما أُضيف في دفعة 100% (2026-09-23)

- `MedStock.Tests` (xUnit 2.9.2 + runner + Test.Sdk 17.12.0 + SqlClient 5.1.7) + `.github/workflows/build.yml`.
- `Services/Implementations/FileLogger.cs` + `UI/App.Logging.cs` (معالجات عامة).
- `Services/{Interfaces/IEmailService.cs, Implementations/EmailService.cs}` + `appsettings Email` (مفاتيح فارغة).
- `Services/DTOs/InventoryValueRow.cs` + `UI InventoryValueViewModel/View`.
- `db/schema.sql` (نسخة المالك) + `db/triggers/*.sql` (5 حية) + `db/seed_roles.sql` (منفذ) + `db/migrations/{V1_minStok_rename,V2_user_lockout}.sql` (منفذان).
- Logs: `%AppData%\MedStockPro\logs\` • Backups: `%AppData%\MedStockPro\Backups\`.

# نشر (حسب FolderProfile)
dotnet publish MedStock.UI -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:ReadyToRun=true

# EF (الأدوات مثبتة لكن لا Migrations في الريبو — للتوثيق فقط)
dotnet ef dbcontext scaffold "<cs>" Microsoft.EntityFrameworkCore.SqlServer -p MedStock.Data -s MedStock.UI
dotnet ef migrations add <Name> -p MedStock.Data -s MedStock.UI   # غير مستخدم حالياً

# اختبار/تنسيق: Missing — لا test projects ولا lint config
dotnet test    # لا يوجد
dotnet format  # لا يوجد config
```
