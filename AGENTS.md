# AGENTS — تعليمات صارمة لأي وكيل AI يعمل في MedStock

> الهوية: **Principal WPF/EF Engineer** — تلتزم بالطبقات المثبتة. ممنوع اختراع API/DB/مكتبات.

## 1. Agent Identity & Role

- احترم `UI → Services → Data → SQL Server`. لا تتجاوز طبقة.
- WPF MVVM فقط: View = XAML + `Loaded→InitAsync`؛ المنطق في `Services/Implementations`؛ لا SQL في VM (استثناء وحيد مثبت: `DatabaseSetupViewModel` يختبر `SqlConnection`).
- حافظ على `ar-IQ` + `RTL` + رسائل عربية + `Guard` + `DbExecutor` + Auditing.
- كل كتابة DB عبر `DbExecutor.ExecuteAsync` (يفتح Transaction + ExecutionStrategy). القراءات الخفيفة عبر `HospitalDbContextFactory` مسموحة (مثل `AlertsService`).
- لا تغيّر `HospitalInventoryDbContext.cs` المولّد إلا عبر Partial (`Auditing/Extensions`) — أي تغيير Schema = سكربت SQL خارجي + إعادة Scaffold، لا `EnsureCreated`.

## 2. Core Rules — DOs / DON'Ts

**DO:**
- اقرأ `PROJECT_MAP.md` + `ARCHITECTURE.md` + الواجهة (`Interfaces/I*.cs`) + DTO قبل أي تعديل.
- استخدم `Guard.Positive/NotNull/NotEmpty` أول أي Service method.
- مرر `CurrentUser.UserId` لـ `SetAuditUser` عبر `DbExecutor` (النمط في `InventoryService.StockInAsync:45`).
- استخدم ثوابت `TransactionReasons` (لا Strings حرفية) + `TransactionNoGenerator` للأرقام.
- FEFO للصرف: `Expiry(null-last) → ReceivedDate → BatchId` كما في `InventoryService.StockOutAsync:158-164`.
- الصلاحيات: `CurrentUser?.IsInRole("Admin",...)` في `CanExecute` + قاعدة bootstrap (بلا أدوار = سماح مؤقت) — انظر `MainWindowViewModel.IsAdmin`.
- حافظ على `FlowDirection=RightToLeft` + مفاتيح `Resources/Strings.*` إن وُجدت؛ وإلا literal عربي متسق.
- سجّل كل VM جديد في `App.xaml.cs` كـ `Singleton` + قالب `DataTemplate` في `App.xaml`.
- الأخطاء: `FileLogger.Error(msg, ex)` في أي catch خدمي جديد؛ VM يعرض `StatusMessage`.
- البريد: `IEmailService` فقط (يعمل فقط مع `Email:ApiKey`؛ بدونه يُسجَّل تخطي) — لا مفاتيح في الريبو أبداً.

**DON'T:**
- ❌ لا تضع منطق أعمال في `Views/*.xaml.cs` (فقط `Loaded→VM` + `Login_Click` + `PasswordChanged` المثبتة).
- ❌ لا تنشئ `DbContext` بـ `new` في UI/Services — استخدم `DbExecutor` أو `HospitalDbContextFactory`.
- ❌ لا تعدّل `Batches.CurrentQty` يدوياً — الرصيد يتحرك عبر `trg_TransactionDetails_StockLedger` (خارج الريبو).
- ❌ لا تضف `Migrations/` أو `EnsureCreated/Migrate()` دون موافقة — القاعدة Database-First.
- ❌ لا تضف حزم جديدة (`Serilog`, `MediatR`, `FluentValidation`…) دون طلب — المعتمد في `TECH_STACK.md` فقط.
- ❌ لا تربط `PasswordBox.Password` بـ Binding — التمرير كـ `CommandParameter` فقط.
- ❌ لا تحذف `Item` له حركات (`ItemsService` يمنع: Batches/RequisitionDetails/StocktakeDetails) — استخدم `SetActiveAsync`.
- ❌ لا تستخدم `DateTime.Now` جديدة — اتبع الخليط القائم (`Now` للكتابة) ولا تخلط `UtcNow` عشوائياً.

## 3. Implementation Workflows (Checklists إلزامية)

### A. إضافة Feature/Entity جديدة
1. اقرأ: `Entities/*.cs` المشابه + `DTOs/*` + `Interfaces/I*.cs` + `DbExecutor.cs` + VM مشابه.
2. إن تطلبت جدولاً: **توقف واسأل** — لا Schema جديد بدون SQL خارجي (Triggers/Defaults/UQ).
3. أنشئ `DTO (ListRow + UpsertRequest)` في `Services/DTOs/` → واجهة في `Interfaces/` → تنفيذ في `Implementations/` (Guard + `DbExecutor` + `EfErrorTranslator` تلقائي).
4. سجّل Service كـ `Singleton` في `App.xaml.cs` (إن جديد).
5. أنشئ `ViewModel: ViewModelBase` (`InitAsync/RefreshAsync`, `RelayCommand`, فحص `IsAuthenticated`) → سجّله Singleton → أضف `DataTemplate` في `App.xaml` → أنشئ `View.xaml` (RTL + DataGrid teal header) + code-behind `Loaded` فقط → أضف زر تنقل في `MainWindow.xaml` + Command في `MainWindowViewModel`.
6. تحقق: `dotnet build MedStock.sln` (0 Errors) + تجربة يدوية (إدخال/صرف/تقرير) + حدّث `PROJECT_MAP.md [ORPHANS & PENDING]`.

### B. تعديل API Endpoint / UI View موجود
1. اقرأ: VM + XAML + Service + DTO المرتبط. تحقق من الروابط الميتة (مثال `RequisitionDetailsView` → لا `DetailsTitle/SaveHeaderCommand`).
2. لا تغيّر توقيع Interface دون تحديث كل المستهلكين (`grep`).
3. حافظ على `CanExecute` + `StatusMessage` العربية + `IsBusy`.
4. إن مسست `Transaction/Requisition/Stocktake` الحالات: احترم `EnsureEditable` + `REQ_FULFILL` gate + `Fulfilled` auto-close.
5. ابنِ وتحقق يدوياً؛ لا Regression (جرب الدخول + لوحة القيادة + الشاشة المعدلة).

### C. Refactor / Tests
1. لا Tests موجودة — أي Refactor يجب أن يسبقه `dotnet build` قبل وبعد + جرد `grep` للمراجع.
2. لا تنقل ملفات بين المشاريع (يفسد `ProjectReference`). الملفات اليتيمة (`Class1.cs`, `Repositories/` الفارغ، `Strings.Designer.cs` الجذري) تُحذف فقط بعد تأكيد عدم الإشارة إليها.
3. إصلاح Warnings: عالج `nullable` المعلنة في `ERRORS_FOUND.md` واحدة واحدة، لا `!` عشوائي.
4. حدّث `PROJECT_MAP.md` ديناميكياً بعد كل خطوة.

## 4. Context Loading Protocol — ماذا تقرأ أولاً حسب المجال

| المجال | اقرأ أولاً |
|--------|------------|
| مخزون (In/Out) | `Interfaces/IInventoryService.cs` + `Implementations/InventoryService.cs` + `DTOs/StockIn-OutRequest.cs` + `DTOs/TransactionReasons.cs` + `Entities/Batch.cs, Transaction*.cs` |
| طلبات الأقسام | `IRequisitionsService.cs` + `RequisitionsService.cs` + `DTOs/RequisitionDtos.cs, PendingRequisitionRow.cs` + `ViewModels/Requisitions*` + `RequisitionContext.cs` |
| جرد | `IStocktakeService.cs` + `StocktakeService.cs` + `DTOs/StocktakeDtos.cs` + `Entities/Stocktake*.cs` |
| تقارير/تنبيهات | `IReportsService.cs` + `ReportsService.cs` + `DTOs/ReportDtos.cs, StockCardDtos.cs, ExpiryReportRow.cs` + `IAlertsService.cs` |
| أصناف/تصنيفات | `IItemsService.cs` + `ItemsService.cs` + `DTOs/ItemDtos.cs, ItemFilter.cs` + `Entities/Item.cs, Batch.cs` |
| مستخدمون/أدوار | `IUserService.cs` + `UserService.cs` + `PasswordHasher.cs` + `IUsersManagementService.cs` + `Entities/User.cs, Role.cs` + `ViewModels/LoginViewModel.cs, UsersViewModel.cs` |
| تدقيق | `Context/HospitalInventoryDbContext.Auditing.cs` + `IAuditService.cs` + `AuditService.cs` + `DTOs/AuditLogListRow.cs` |
| إعداد/تشغيل | `App.xaml.cs` + `App.xaml` + `MainWindow*` + `ViewModels/DatabaseSetupViewModel.cs` + `appsettings.json` |
| توطين/ثيم | `Resources/Strings.resx` + `Resources/Strings.ar.resx` + `Styles/Colors.xaml` + `ILanguageService.cs` + `LanguageService.cs` |

## 5. أسئلة إلزامية — توقف واسأل ولا تخمن

- أي غموض في `ReasonCode/Scope` أو حالة `Requisition/Stocktake` جديدة → اسأل.
- أي Schema/Trigger/SQL مفقود (`trg_*`، `MinStok` typo، `StocktakeDetailId` بلا nav) → اسأل قبل الكود.
- أي مكتبة/إصدار غير مثبت في `TECH_STACK.md` → اسأل.
- أي `ActivityType` غير `Hospital` (مخزن تجاري) → اسأل عن labels (`DepartmentLabel/RequisitionLabel`).
