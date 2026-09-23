# PROJECT_MAP — MedStock (Hospital Inventory / WPF)

> مصدر الحقيقة: فحص مباشر للملفات. لا تخمين. تاريخ الفحص: 2026-09 (أمر `Get-Date`) — SDK المثبت `9.0.318` — المستهدف `net8.0-windows`.
> الحل: `MedStock.sln` (VS 17.14) — 3 مشاريع فقط. لا CI/CD، لا Docker، لا Tests، لا Migrations داخل الريبو.

## 1. Directory Hierarchy — مسؤولية كل مجلد

```
MedStock/
├── MedStock.sln                          # Solution: 3 مشاريع (Data, Services, UI)
├── .gitignore                            # bin/, obj/, .vs/, *.user, appsettings.json مستثنى
├── MedStock.Data/                        # طبقة البيانات (EF Core Database-First / Scaffold)
│   ├── MedStock.Data.csproj
│   ├── Context/
│   │   ├── HospitalInventoryDbContext.cs            # Scaffold: 18 DbSet + OnModelCreating (Indexes, UQ, Defaults, Triggers names)
│   │   ├── HospitalInventoryDbContext.Auditing.cs   # Partial: CurrentUserId/SetAuditUser + SaveChangesAsync -> AuditLogs
│   │   ├── HospitalInventoryDbContext.Extensions.cs # Partial: ConfigureDefaults (LazyLoading=false)
│   │   └── DbContextFactory.cs                     # IDbContextFactory<T> مخصص + HospitalDbContextFactory
│   └── Entities/ (18 ملف)
│       ├── AuditLog, User, Role, UserRole
│       ├── Item, Category, ItemCategory (M:N)
│       ├── Batch (ItemId+BatchCode UQ)
│       ├── Transaction, TransactionDetail, TransactionReason
│       ├── Requisition, RequisitionDetail, RequisitionFulfillmentLink
│       ├── Department, Supplier
│       └── Stocktake, StocktakeDetail (Difference عمود محسوب)
├── MedStock.Services/                    # طبقة منطق الأعمال (Services + DTOs + Interfaces)
│   ├── MedStock.Services.csproj (net8.0-windows + UseWPF=true)
│   ├── Class1.cs                         # فارغ — يتيم (انظر ORPHANS)
│   ├── Interfaces/ (15): IAlerts, IAudit, ICategories, IDepartments, IInventory,
│   │                  IItemCategories, IItems, ILanguage, IReports, IRequisitions,
│   │                  ISessionContext, IStocktake, ISuppliers, IUser, IUsersManagement
│   ├── DTOs/ (21 ملف): CategoryDtos, DepartmentDtos, ItemDtos, ItemFilter, PagedResult,
│   │                  SupplierDtos, StocktakeDtos, StockCardDtos, ReportDtos (StockCard/Consumption),
│   │                  RequisitionDtos, PendingRequisitionRow, MinStockAlertRow, ExpiryReportRow,
│   │                  AuditLogListRow, StockInRequest, StockOutRequest, SessionUser,
│   │                  UserDtos, ItemCategoryDtos, TransactionReasonDto, TransactionReasons (ثوابت)
│   └── Implementations/ (20):
│       ├── DbExecutor.cs                 # غلاف Transactions + ExecutionStrategy + EfErrorTranslator
│       ├── Guard.cs, EfErrorTranslator.cs
│       ├── SessionContext.cs (ISessionContext)
│       ├── UserService.cs (AuthenticateAsync PBKDF2)
│       ├── PasswordHasher.cs (internal, PBKDF2-SHA512 100k, salt32/hash64)
│       ├── InventoryService.cs (StockIn/StockOut FEFO + REQ_FULFILL ربط)
│       ├── RequisitionsService.cs (Draft->Submitted->Approved/Rejected->Fulfilled, FulfillLineAsync)
│       ├── StocktakeService.cs (CreateDraft snapshot + SaveCounts + Post تسويات)
│       ├── ItemsService.cs, CategoriesService.cs, ItemCategoriesService.cs
│       ├── DepartmentsService.cs, SuppliersService.cs
│       ├── UsersManagementService.cs, AlertsService.cs, ReportsService.cs (StockCard/Expiry/Consumption)
│       ├── AuditService.cs, LanguageService.cs (ActivityType)
│       └── TransactionNoGenerator.cs (TRX-yyyyMMdd-HHmmss-rand)
└── MedStock.UI/                          # WPF MVVM (WinExe net8.0-windows)
    ├── MedStock.UI.csproj (+ ProjectRef -> Services)
    ├── appsettings.json                  # فقط ConnectionStrings:HospitalDb (SQLEXPRESS/HospitalInventoryDb)
    ├── App.xaml / App.xaml.cs            # Bootstrap + Host + DataTemplates (18) + ثقافة ar-IQ + RTL
    ├── MainWindow.xaml / .xaml.cs        # Shell: Sidebar 260px + ContentControl
    ├── AssemblyInfo.cs ([ThemeInfo])
    ├── Strings.Designer.cs               # قديم/فارغ internal — يتيم
    ├── Assets/                           # فارغ (Folder Include فقط)
    ├── Converters/                       # BooleanToVisibility, InvertBool
    ├── Resources/
    │   ├── Strings.resx / Strings.ar.resx / Strings.Designer.cs (فعلي، ~50 مفتاح)
    │   └── Styles/Colors.xaml            # Teal theme (#00695C …)
    ├── Properties/PublishProfiles/FolderProfile.pubxml (+ .user) # SelfContained win-x64 SingleFile
    ├── ViewModels/ (28): ViewModelBase, RelayCommand(+Generic, ns MedStock.UI),
    │   INavigationService, NavigationService, ConfigFilePointer, RequisitionContext,
    │   MainWindow, Login, Dashboard, Items, StockIn(+StockInLineVm), StockOut,
    │   Categories, ItemCategories(+CheckRowVm), MinStockAlerts, Suppliers,
    │   Users(+RoleCheckVm), StockCard, Stocktakes, StocktakeDetails(+StocktakeLineVm),
    │   Requisitions, RequisitionDetails, ExpiryReport, Departments, AuditLogs,
    │   ConsumptionReport, DatabaseSetup
    └── Views/ (20 xaml + 20 cs + StatusToVisibilityConverter.cs):
        Login, Dashboard, Items, StockIn, StockOut, Categories, ItemCategories,
        MinStockAlerts, Suppliers, Users, StockCard, Stocktakes, StocktakeDetails,
        Requisitions, RequisitionDetails, Departments, ExpiryReport, AuditLogs,
        ConsumptionReport, DatabaseSetup (الوحيدة Window; الباقي UserControl)
```

## 2. [TECH_STACK] (ملخص — التفصيل في `TECH_STACK.md`)

- Runtime: `net8.0-windows` (UI WinExe + Services UseWPF) / `net8.0` (Data). SDK المرصود `9.0.318`.
- ORM: `Microsoft.EntityFrameworkCore 8.0.23` + `SqlServer 8.0.23` + `Tools 8.0.23` + `Design 8.0.23`.
- Hosting/DI: `Microsoft.Extensions.Hosting 8.0.1` + `DI 10.0.2` + `Configuration.Json 8.0.1`.
- Logging: `FileLogger` داخلي (async queue → `%AppData%\MedStockPro\logs\`) — بلا حزم خارجية.
- Email: `SendGrid 9.29.3` مفعّل عبر `IEmailService` (يعمل فقط مع `Email:ApiKey`).
- Tests: `MedStock.Tests` (xUnit 2.9.2) — 23/23 خضراء. CI: `.github/workflows/build.yml` (windows-latest).
- DB: SQL Server (`.\SQLEXPRESS`, `HospitalInventoryDb`) — Database-First + `db/` (schema/triggers/migrations V1/V2).
- أوامر: `dotnet build MedStock.sln` (0 Errors) + `dotnet test` — التفاصيل في `ERRORS_FOUND.md`.

## 3. Entry Points

| # | الملف | الدور |
|---|-------|-------|
| 1 | `MedStock.UI/App.xaml.cs:OnStartup` | يضمن `%AppData%\MedStockPro\appsettings.json`، يفرض ثقافة `ar-IQ` + RTL، يبني `IHost`، يستدعي `CheckDatabaseAndRun()` |
| 2 | `MedStock.UI/App.xaml.cs:CheckDatabaseAndRun` (`async void`) | `CanConnectAsync` عبر `HospitalDbContextFactory`؛ فشل → `DatabaseSetupView`؛ نجاح → `MainWindow` |
| 3 | `MedStock.UI/MainWindow.xaml` + `MainWindowViewModel.cs:55` | Shell + `NavigateTo<LoginViewModel>()` كبداية؛ `ContentControl Content={Binding CurrentViewModel}` يعرض DataTemplates من `App.xaml` (18 قالب) |
| 4 | `MedStock.UI/ViewModels/DatabaseSetupViewModel.cs` | شاشة أول تشغيل: Test/Save لسلسلة الاتصال + `ActivityType` |

## 4. [SYSTEM_FLOW] — رحلة الطلب (WPF، لا API/Controllers)

```
User (XAML View, RTL ar-IQ)
  -> Binding -> ViewModel (RelayCommand<string>/RelayCommand, CanExecute)
  -> Guard (Services/Implementations/Guard.cs) + SessionContext.IsAuthenticated فحص
  -> Service Interface (مثال IInventoryService.StockInAsync)
  -> DbExecutor.ExecuteAsync<T> (ExecutionStrategy + BeginTransaction + SaveChanges + Commit)
       -> HospitalInventoryDbContext (DbContextFactory.CreateDbContext, SetAuditUser)
       -> SQL Server (جداول + Triggers خارج الريبو: trg_TransactionDetails_StockLedger …)
       -> SaveChangesAsync (Auditing partial) -> AuditLogs.Add + SaveChanges ثانية
  -> DTO/Row يُرجع (مثال TransactionId / PagedResult<ItemListRow>)
  -> ViewModel يحدّث Observable props (SetProperty) / StatusMessage عربي
  -> View يتحدث عبر INPC/DataTemplate؛ أخطاء تُعرض كنص (ErrorMessage/StatusMessage)، لا Dialog مركزي
```

أمثلة مثبتة:
- دخول: `LoginView (PasswordBox->Login_Click->LoginCommand.Execute(Pwd.Password))` → `UserService.AuthenticateAsync` (PBKDF2 verify + Roles join) → `SessionContext.SetUser` → `SessionChanged` → `MainWindowViewModel` ينتقل لـ `DashboardViewModel.InitAsync` (alerts + Submitted + expiry90).
- صرف مرتبط بطلب: `StockOutView (PendingRequests)` → `RequisitionsService.FulfillLineAsync` → `InventoryService.StockOutAsync (RE Q_FULFILL + FEFO Expiry→Received→BatchId)` → `RequisitionFulfillmentLinks` → إن اكتملت السطور `Status=Fulfilled`.
- جرد: `StocktakesView.CreateDraft` (snapshot كل `Items.IsActive` + `SystemQty` من `Batches`) → `StocktakeDetailsView.Save (SaveCountsAsync)` → `PostAsync` (فروق +→`StockIn PURCHASE`، −→`StockOut ADJ_OUT`) → `Status=Posted`.

## 5. [ARCHITECTURE] (ملخص — التفصيل في `ARCHITECTURE.md`)

- النمط: Layered MVVM (UI ↔ Services ↔ Data) — لا Clean Architecture صارم، لا Repository/UnitOfWork مجردين، لا CQRS، لا MediatR.
- الحدود: UI يملك `ProjectReference → Services → Data`. لا وصول مباشر UI→Data except `DbContextFactory` للـ `CanConnect` فقط في `App.xaml.cs`.
- DI: كل التسجيلات `Singleton` في `App.xaml.cs:ConfigureServices` (بضمنها `AddDbContext` الافتراضي Scoped + ViewModels + 4 Views فقط؛ الباقي عبر DataTemplates). `DbExecutor` + `HospitalDbContextFactory` هما بوابة الكتابة.
- Cross-cutting: `Guard` + `EfErrorTranslator` (تمرير `DbException.Message` خام) + `HospitalInventoryDbContext.Auditing` (Create/Update/Delete → `AuditLogs`) + `LanguageService (ActivityType: Hospital|غيره)` + ثقافة `ar-IQ` قسرية. لا Logging framework (فقط `Debug.WriteLine` في Dashboard + `StatusMessage` نصية). لا Validation pipeline مركزي. لا Authorization بالأدوار على مستوى UI (كل القوائم ظاهرة بعد الدخول؛ فقط حالات الطلب `CanEdit/CanApprove/...`).
- الأمان/الشبكة: PBKDF2-SHA512 (100k) + `FixedTimeEquals`؛ كلمة المرور لا تُربط (تُمرر كـ CommandParameter) except `UsersView (TextBox)`؛ لا JWT/OAuth؛ لا CORS (تطبيق Desktop)؛ لا تشفير إضافي؛ الأسرار في `%AppData%\MedStockPro\appsettings.json` (غير مُتجاهل بالكامل — `.gitignore` يتجاهل `appsettings.json` الجذري فقط).

## 6. Key Modules & Responsibilities

| الوحدة | المسؤولية | المستهلك |
|--------|-----------|----------|
| `MedStock.Data/Context` | تعريف Schema (18 كيان، UQ/IX/Defaults/Triggers names)، Auditing، Factory | Services فقط |
| `MedStock.Data/Entities` | POCOs + Navigations (Item↔Batch↔TransactionDetail، Requisition chain، Stocktake) | Services |
| `Services/Implementations` | قواعد المخزن: FEFO، أرقام فريدة، حالات الطلب/الجرد، تقارير، تنبيهات، مصادقة | ViewModels |
| `Services/DTOs` | `*ListRow/*UpsertRequest/PagedResult/ItemFilter/StockIn-OutRequest/SessionUser/TransactionReasons` | VM↔Service contracts |
| `UI/ViewModels` | Binding state + Commands + `InitAsync/RefreshAsync/LoadDataAsync` + رسائل عربية | Views |
| `UI/Views` | XAML RTL + DataGrids + DataTemplates؛ code-behind فقط `Loaded→VM` (+Login/DatabaseSetup استثناء) | المستخدم |
| `UI/Resources+Converters` | `Colors.xaml` + `Strings.*` + `BooleanToVisibility/InvertBool` | كل Views |

## 7. [ORPHANS & PENDING] — النواقص المتتبعة (تُحذف عند الاكتمال)

> التفصيل الكامل: `TODO.md` (مهام) + `ERRORS_FOUND.md` (أخطاء) + `MISSING_FEATURES.md` (ميزات مقارنة).
> آخر تحديث: 2026-09-23 — أُنجزت الدفعة الأولى (4 وكلاء متوازيين + توحيد). البناء: 0 Errors / 16 Warnings.

**تم (2026-09-23، متحقق بالبناء والـ DB الحية + 23 اختباراً):**
- [x] T01 `GetReasonsAsync` • T02 روابط `RequisitionDetailsView` • T03 توثيق DB + seed أدوار • T04 عنوان المورد
- [x] T05/T06/T07/T08/T09 (UI/ربط) • T10 يتامى • T11 `ConfigFilePointer` • T12 توثيق DI • T13 SendGrid مفعّل • T14 `MinStock` موحد • T15 أرشيف موثق • T16 معالجات عامة • T17 زمن محلي • T18 `getdate`→`sysutcdatetime`
- [x] T19 IP/Machine • T20 قفل + `LastLoginAt` + سياسة 8 أحرف • T21 `PasswordBox` • T22/T23 جرد ذري
- [x] RBAC: `IsInRole` + بوابات (اعتماد/صرف/ترحيل/مستخدمون) + إخفاء قائمة المستخدمين + قاعدة bootstrap
- [x] تقارير: قيمة المخزون + CSV (4 شاشات) + طباعة + رسوم الأعلى استهلاكاً + ملخص بريد + نسخ احتياطي `.bak`
- [x] Logging ملف async + `MedStock.Tests` (23/23) + CI + `db/migrations` (V1/V2)

**متبقٍ — يدوي فقط (دقائق في التطبيق الحي):**
- [ ] إسناد أدوار للمستخدمين الثلاثة من شاشة المستخدمين (قاعدة bootstrap تسمح بالدخول)
- [ ] M1: إدخال → طلب → اعتماد → صرف → بطاقة الصنف (قبول نهائي)
- [ ] تعبئة `Email:ApiKey/FromEmail/AdminEmail` في `%AppData%\MedStockPro\appsettings.json` لتفعيل البريد (بدونه يُسجَّل تخطي في الـ Log)
- [ ] دين واحد صغير موثق: إزالة `Newtonsoft.Json` من `*.csproj` عند أول تنظيف
