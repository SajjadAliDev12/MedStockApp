# ARCHITECTURE — MedStock

> كل بند مستخرج من الملفات. ما لم يوجد موثق كـ **Not Implemented / Missing**.

## 1. Architectural Style

- **Layered MVVM (WPF)**: `MedStock.UI (Views+ViewModels)` → `MedStock.Services (Interfaces+Implementations+DTOs)` → `MedStock.Data (DbContext+Entities)` → SQL Server.
- **ليس** Clean/Hexagonal: لا `Core/Domain` مستقل، لا `Repository/IUnitOfWork` مجردين، لا CQRS/MediatR، لا API Controllers (تطبيق Desktop مباشر).
- **Tenets الفعلية**: Singleton DI لكل شيء؛ `DbExecutor` كبوابة كتابة وحيدة (ExecutionStrategy + Transaction)؛ `IDbContextFactory` مخصص للقراءات الخفيفة (`AlertsService`, `StocktakeService.GetList/GetDetails`)؛ Auditing داخل `DbContext.SaveChangesAsync`؛ ثوابت الأسباب `TransactionReasons` كعقد نصي مع جدول `TransactionReasons`.
- **DB approach**: Database-First / Scaffold (`HospitalInventoryDbContext.cs` مولّد: `PK__…` names، `HasDefaultValueSql(sysutcdatetime()/getdate())`، `HasTrigger(...)` أسماء فقط). لا `Migrations/` في الريبو — أي تغيير Schema يتطلب Scaffold يدوي + سكربت SQL خارجي.

## 2. Component Breakdown & Boundaries

| الطبقة | المشروع/المجلد | ما يملكه | ما يُمنع عليه |
|--------|----------------|----------|----------------|
| Data | `MedStock.Data/Context/*.cs`, `Entities/*.cs` | 18 `DbSet`، `OnModelCreating` (UQ/IX/Defaults/Computed `Difference`)، `Auditing partial`، `HospitalDbContextFactory` | لا منطق أعمال (عدا اشتقاق `AuditLog` من `AuditEntry`) |
| Services | `MedStock.Services/Interfaces/*.cs` (15) | عقود `Task`-based + `CancellationToken` | لا XAML/WPF types |
| Services | `Implementations/*.cs` (20) | `DbExecutor`، `Guard`، `SessionContext`، `UserService`+`PasswordHasher`، `Inventory/Requisitions/Stocktake/Reports/Alerts/...` | لا `MessageBox`/`SaveFileDialog` (يملكها `ConsumptionReportViewModel` فقط — مسموح كاستثناء UI) |
| Services | `DTOs/*.cs` (21) | `*ListRow` للقراءة، `*UpsertRequest` للكتابة، `PagedResult<T>`، `ItemFilter`، `SessionUser` | لا Entities مباشرة للـ UI |
| UI | `ViewModels/*.cs` (28) | `ViewModelBase(INPC)`، `RelayCommand` (ns `MedStock.UI`)، `NavigationService`، state + `InitAsync/RefreshAsync` | لا SQL/EF مباشر (عدا `SqlConnection` في `DatabaseSetupViewModel` لاختبار الاتصال — مبرر) |
| UI | `Views/*.xaml` (20) | RTL، DataTemplates في `App.xaml` (18)، `ContentControl` في `MainWindow` | لا منطق (code-behind = `Loaded→VM` فقط + `Login_Click` + `PasswordChanged`) |
| Shared UI | `Converters/`, `Resources/Styles/Colors.xaml`, `Resources/Strings.*` | تحويلات + ثيم Teal + توطين جزئي | — |

**DI الحقيقي** (`App.xaml.cs:71-156`): كل شيء `AddSingleton` — شامل `AddDbContext` (الافتراضي Scoped لكنه يُحقن في Singletons عبر `DbExecutor/Factory`؛ `AuditService` يستخدم `DbExecutor` لا `DbContext` مباشرة — لا Captive-DbContext مباشر مرصود، لكن التصميم هش: `StocktakeService` يحقن `DbExecutor+IInventory+Factory` معاً). `ConfigFilePointer` مسجل مرتين (factory + instance — الثاني يفوز). `App.ConfigFilePointer` class مكرر غير مستخدم.

**Navigation**: `INavigationService.NavigateTo<T>` (ServiceLocator عبر `IServiceProvider`) + `CurrentChanged` → `MainWindowViewModel.CurrentViewModel` → `DataTemplate`. استثناءان للحالة العابرة: `StocktakesViewModel.SelectedStocktakeIdForDetail` (static) و `RequisitionContext.CurrentRequisitionId` (singleton).

## 3. Database Architecture

**الكيانات (18)** — الخصائص المثبتة عبر `Select-String public.*{ get`:
- `User (UserId, Username UQ, DisplayName, PasswordHash byte[64], PasswordSalt byte[32], IsActive, CreatedAt, LastLoginAt)` ↔ `Role` عبر `UserRole (UserId+RoleId PK)`.
- `Item (ItemId, SKU UQ, ItemName, UnitOfMeasure, ReorderLevel, MinStok [typo مثبت], IsActive)` ↔ `Category` عبر `ItemCategory (ItemId+CategoryId PK)`.
- `Batch (BatchId, ItemId+BatchCode UQ, ReceivedDate, ExpiryDate?, InitialQty, CurrentQty, UnitCost, LocationCode)` ↔ `TransactionDetail`.
- `Transaction (TransactionId, TransactionNo UQ, Type char(1): I/O, Date, Dept?, User?, Reason?, Supplier?)` → `TransactionDetail (TransactionId+BatchId, Quantity, UnitCost, SupplierRef?, StocktakeDetailId?)`.
- `TransactionReason (ReasonId, Code UQ, Name, Scope char(1), IsSystem, IsActive)` — القيم النصية المتوقعة من `TransactionReasons.cs`: `PURCHASE/DONATION/RETURN_IN/ADJ_IN/INITIAL | DISPENSE/EXPIRED/DAMAGE/ADJ_OUT/REQ_FULFILL/STOCKTAKE_OUT/STOCKTAKE_IN`.
- `Requisition (RequisitionNo UQ, Dept, Status string: Draft/Submitted/Approved/Rejected/Cancelled/Fulfilled, RequestedBy/ApprovedBy, RequestDate, DecisionDate?)` → `RequisitionDetail (RequisitionId+ItemId UQ, RequestedQty, FulfilledQty)` → `RequisitionFulfillmentLink (RequisitionDetailId+TransactionDetailId UQ)`.
- `Stocktake (StocktakeNo UQ, Date, Status: Draft/Posted/Cancelled, CreatedBy/PostedBy/CancelledBy)` → `StocktakeDetail (StocktakeId+ItemId UQ, SystemQty, PhysicalQty?, Difference محسوب `isnull(Physical)-System`)`.
- `Department (Code UQ, Name UQ, IsActive)`، `Supplier (Name, Phone, Email, Address, IsActive)`، `AuditLog (OccurredAt, UserId?, EntityName, EntityId(50), ActionType(30), Summary(300), DetailsJson, Ip, Machine)`.

**علاقات حرجة**: `TransactionDetail.Batch (ClientSetNull)`، `Batch.Item`، `RequisitionFulfillmentLink` جسر الصرف↔الطلب، `StocktakeDetail.ReasonId→TransactionReason` (nullable).

**Indexes**: `IX_AuditLogs_{Entity,OccurredAt,UserId_OccurredAt}`، `UQ_Batches_Item_Batch`، `UQ_Items_SKU`، `IX_Requisitions_{Department_Status,Status_Date}`، `IX_RFL_*`، `IX_Transactions_*`، `IX_Users_IsActive` إلخ — كلها في `HospitalInventoryDbContext.cs:57-446`.

**Migration strategy**: **Missing** — لا `Migrations/`، لا `EnsureCreated/Migrate()` (grep صفر). الاعتماد على DB خارجية + Triggers خارج الريبو:
- `trg_TransactionDetails_StockLedger` (على `Transaction_Details`) — **حرج**: `InventoryService` لا يخصم/يضيف `Batches.CurrentQty` يدوياً بل يعتمد عليه.
- `trg_RFL_UpdateFulfilledQty` + `trg_RFL_ValidateLink` (على `RequisitionFulfillmentLink`)، `trg_Transactions_ValidateReasonScope` (على `Transactions`).
- خطر مثبت: `StocktakeService.PostAsync` يستدعي `StockIn (PURCHASE, UnitCost=0, BatchCode=ADJ-yyMMdd, Expiry=+1y)` و `StockOut (ADJ_OUT)` — إن غاب Trigger أو `ReasonCode` من جدول الأسباب فشلت العملية.

## 4. Cross-Cutting Concerns

| الاهتمام | الواقع | الموقع |
|----------|--------|--------|
| Logging | `FileLogger` (async queue → `%AppData%\MedStockPro\logs\medstock-*.log`, مستويات Info/Warn/Error، لا يرمي أبداً) + `DbExecutor` يسجل كل كسر + معالجات عامة (`App.Logging.cs`) + `StatusMessage/ErrorMessage` نصية في كل VM | `FileLogger.cs`, `DbExecutor.cs:41-44,70-73`, `App.Logging.cs` |
| Exception handling | `DbExecutor` + `EfErrorTranslator` + `FileLogger` + عرض `ex.Message` في VM. لا `Result<T>` (موثق كقرار). | `DbExecutor.cs` |
| Validation | `Guard` + سياسة كلمات (8 أحرف) + فحوص عربية + `CanExecute` (+ أدوار). | `Guard.cs`, `UsersManagementService.cs` |
| State sync | `ISessionContext (CurrentUser/SessionChanged)` + `INavigationService.CurrentChanged` + `INPC.SetProperty`. لا EventAggregator/Messenger | `SessionContext.cs`, `NavigationService.cs` |
| Time | **سياسة: التوقيت المحلي للخادم.** كل الكتابة `DateTime.Now`؛ `sysutcdatetime()` defaults احتياطية فقط. | `InventoryService`, `Auditing.cs:155` |
| i18n | قسري `ar-IQ` + `FlowDirection=RTL` + `Strings.resx/ar` (~50 مفتاح، استخدام جزئي) + `LanguageService(ActivityType)` يبدل `Department/Requisition/Activity` labels. الباقي literals عربية في XAML/VMs | `App.xaml.cs:50-62`, `LanguageService.cs` |

## 5. Security & Networking

- **Auth flow**: `LoginView.PasswordBox → LoginCommand<string> → UserService.AuthenticateAsync` (قفل 5 محاولات/15 دقيقة + `LastLoginAt` + PBKDF2-SHA512/100k/`FixedTimeEquals`) → `SessionUser{...,Roles}` → `SessionContext.SetUser`. سياسة كلمات: 8 أحرف min عند الإنشاء/التعيين.
- **Authorization**: `SessionUser.IsInRole(...)` + `MainWindowViewModel.IsAdmin` (إخفاء قائمة المستخدمين) + بوابات: اعتماد/رفض (StoreManager/Admin)، صرف السطر (Storekeeper/+/Admin)، ترحيل الجرد (StoreManager/Admin)، شاشة المستخدمين (Admin). قاعدة bootstrap: مستخدم بلا أدوار (legacy) يُعامل كـ Admin حتى تُسند الأدوار.
- **Transport/Network**: N/A (Desktop). لا JWT/Cookies/OAuth، لا CORS. سلسلة الاتصال `Trusted_Connection=True; TrustServerCertificate=True` (تجاوز تحقق الشهادة — مقبول محلياً فقط).
- **Secrets**: `%AppData%\MedStockPro\appsettings.json` (يُنسخ من CWD أول مرة؛ `DatabaseSetupViewModel` يكتبه). `.gitignore` يتجاهل `appsettings.json` الجذري لكن `MedStock.UI/appsettings.json` الحالي مُتتبع ويحتوي `.\SQLEXPRESS` (معلومة بيئة محلية فقط، لا كلمة سر).
- **Threats مثبتة**: `UsersView.PasswordText` حقل `TextBox` عادي (ظاهر)؛ `AuditLog.EntityId` يُبنى بـ `JsonSerializer.Serialize(KeyValues).Trim('{','}').Replace('"',"")` مع truncate 50 (هش)؛ `IpAddress/MachineName` في `AuditLog` لا تُملأ من `Auditing partial` (تبقى null — لا `Dns/Ip` capture).
