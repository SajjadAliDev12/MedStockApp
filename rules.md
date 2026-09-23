# rules.md — MedStock (mirror of `.cursorrules`, human-readable)

> القاعدة الذهبية: لا تخترع. كل شيء أدناه مثبت في الملفات (فحص 2026-09).

## 1. التنسيق والأسلوب
- C#: 4 مسافات، أقواس Allman، `file_scoped_namespaces`، `Nullable enable`.
- XAML: كل شاشة `FlowDirection=RightToLeft`، خط `Segoe UI 14`، فرش الثيم من `Resources/Styles/Colors.xaml` (Teal `#00695C`)، رأس DataGrid موحد (انسخ من `ItemsView.xaml`).
- التسمية: `*ViewModel`, `*View`, `I*Service/*Service`, `*ListRow/*UpsertRequest`, `PagedResult<T>`. احتفظ بخطأ `MinStok` كما هو حتى Migration رسمي.
- اللغة: عربي في الواجهة + `Strings.resx/ar.resx` للمشترك (`{x:Static res:Strings.*}`).

## 2. حقن التبعيات
- كل التسجيلات `Singleton` في `App.xaml.cs` (بما فيها `AddDbContext` + كل VMs). أي VM جديد: سجّله + أضف `DataTemplate` في `App.xaml` + زر في `MainWindow` + Command في `MainWindowViewModel`.
- الكتابة فقط عبر `DbExecutor.ExecuteAsync` (Transaction + Retry). القراءة الخفيفة عبر `HospitalDbContextFactory` + `AsNoTracking`.

## 3. معالجة الأخطاء
- ابدأ كل Service بـ `Guard.*`. رسائل المستخدم عربية.
- `DbExecutor` يترجم عبر `EfErrorTranslator` ويعيد `InvalidOperationException`. الـ VM يعرض `ex.Message` في `StatusMessage/ErrorMessage` مع `IsBusy`.
- لا `Result<T>`، لا handler عالمي — لا تعتمد على واحد.

## 4. المكتبات
- المسموح: EF Core `8.0.23` + `Extensions.Hosting 8.0.1 / DI 10.0.2 / Config.Json 8.0.1` + `System.Text.Json` + `SqlClient` لاختبار الاتصال فقط.
- الميت/المقيد: `Newtonsoft.Json`, `SendGrid`, `Azure.Core` (احذف الـ using اليتيم في `RequisitionsService.cs:1`).
- الممنوع دون موافقة: `Serilog/MediatR/FluentValidation` جديدة، `Migrations/EnsureCreated`، مزود DB جديد.

## 5. الحدود
- `UI → Services → Data → SQL` فقط. لا منطق في code-behind (عدا `Loaded` + `Login_Click` + `PasswordChanged`).
- `CurrentQty` يتحرك بالـ Trigger فقط (`trg_TransactionDetails_StockLedger`).
- الحالات نصوص: `Draft/Submitted/Approved/Rejected/Cancelled/Fulfilled` و `Draft/Posted/Cancelled` و `I/O` — احترم البوابات.
- كلمة المرور كـ `CommandParameter` فقط.
