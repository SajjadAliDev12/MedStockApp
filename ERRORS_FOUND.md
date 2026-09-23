# ERRORS_FOUND — أخطاء واجهها الفحص (للمراجعة والإصلاح لاحقاً)

> المنهج: `dotnet build MedStock.sln` + قراءة مباشرة للملفات. البناء الحالي (2026-09-23): **0 Errors / 16 Warnings** (كان 48). البنود الموسومة **[FIXED]** أُصلحت وروُجعت في هذه الدفعة.

## A. أخطاء وظيفية (Bugs مثبتة بالقراءة)

1. **[FIXED 2026-09-23] `GetReasonsAsync` غير منفذة** — نُفذت (فلتر Scope + ترتيب + DTO). تحقق: نطاقات I/O تُرجع 6+6 على DB الحية.
2. **[FIXED 2026-09-23] `RequisitionDetailsView` روابط ميتة** — أُضيف `DetailsTitle/DeptLabel/SaveHeaderCommand` + `CommandParameter` للحذف.
3. **[FIXED 2026-09-23] `MinStockAlertsView` صف خارج النطاق** — `Grid.Row="6"` → `"3"`.
4. **[FIXED 2026-09-23] `DiffColor string→Foreground`** — أصبح `Brush` (`Brushes.*`).
5. **[FIXED 2026-09-23] فقدان عنوان المورد عند التحرير** — `Address` أُضيف للـ Row والتعبئة والـ Setter (المسح في `StartNew()` مقصود).
6. **`StockOut RFL CreatedAt = UtcNow` وحيدة** — `InventoryService.cs:224` (`DateTime.UtcNow`) بينما كل الكتابات `DateTime.Now` → خلط زمني في `RequisitionFulfillmentLinks.CreatedAt`.
7. **[FIXED 2026-09-23] عرض الرصيد المتبقي خاطئ النوع** — أُزيل cast `(Int32)` → `{remaining:0.###}`.
8. **[FIXED 2026-09-23] `Stocktake Post` يستخدم `PURCHASE` للفائض** — أصبح `ADJ_IN` (بقرار المالك) + `BatchCode` فريد لكل صنف + `Expiry=+1y` بقي (يحتاج سياسة نهائية في M2).
9. **`AuditLog.EntityId` هش** — `Context/HospitalInventoryDbContext.Auditing.cs:158-159`: `Json(KeyValues).Trim('{','}').Replace('"',"")` + truncate 50 → IDs مركبة/طويلة تُشوَّه، و`OccurredAt=DateTime.Now` (`:155`) يخالف `sysutcdatetime()` default.
10. **`UsersView` كلمة مرور مكشوفة** — حقل `TextBox` عادي (`Views/UsersView.xaml`) للإنشاء + `ResetPasswordAsync` → تُعرض وتُسجَّل في الذاكرة كنص؛ مقابل `LoginView` الذي يستخدم `PasswordBox` بشكل صحيح.
11. **`MainWindow` زر خروج بقالب مخصص يكسر الثيم** — `MainWindow.xaml:100-109` (`Background #C62828` + Template inline يتجاهل `MenuBtn`) — بصري فقط لكنه drift مثبت.
12. **[FIXED 2026-09-23] DI مزدوج `ConfigFilePointer`** — تسجيل مفرد + حُذف الـ class المكرر.
13. **`Async void` + `RelayCommand(async…)`** — `App.CheckDatabaseAndRun:163` وكل أوامر الحفظ (`SaveCommand(async()=>await SaveAsync())`) fire-and-forget → استثناء غير ممسوك يُسقط التطبيق.
14. **`AddDbContext` الافتراضي Scoped داخل Singletons** — `App.xaml.cs:75` + كل Services Singletons. المستقر حالياً لأن الكتابة عبر `DbExecutor/Factory`، لكن أي حقن مباشر لـ `HospitalInventoryDbContext` في Singleton مستقبلاً = captive dependency.

## B. تحذيرات البناء (48 Warnings — عينة مثبتة، القائمة الكاملة في `dotnet build` output)

- `CS8618` (non-nullable بلا init): `Auditing.cs:144` (`TableName/AuditType`)، `DTOs/TransactionReasonDto.cs:6-7`، `StockOutViewModel.cs:45` (`_selectedItem/_selectedDepartment/_selectedPendingRequest/_searchCts`).
- `CS8601/CS8602/CS8604` (nullability): `Auditing.cs:63,71,76,84,85,114,118`، `AuditService.cs:57,77`، `AlertsService.cs:35` (`MinStok.Value` بلا فحص بعد `where MinStok != null` — آمن منطقياً لكن المترجم يشتكي)، `SuppliersService.cs:33`، `ReportsService.cs:228`، `DatabaseSetupViewModel.cs:121,130,143,146`، `App.xaml.cs:165` (`_host.Services` بلا null-check بعد `Build`)، `StockOutViewModel.cs:59,208,224,253,258,259`.
- `CS0414` (حقل ميت): `UsersViewModel.cs:35` (`_isPasswordMode` never used).
- كلها Warnings لا Errors — لكن `CS8602` في `DatabaseSetupViewModel` و`App` تعني `NullReferenceException` محتملة عند config تالف/host فاشل.

## C. مشاكل معمارية/اعتمادية (تُعامل كأخطاء لاحقاً)

- **Triggers خارج الريبو**: أي بيئة DB جديدة بلا `trg_TransactionDetails_StockLedger` = مخزون لا يتحرك إطلاقاً (الخدمات لا تحدّث `CurrentQty` يدوياً — تعليق مثبت `InventoryService.cs:83-86,179`).
- **`RequisitionService using Azure.Core` يتيم** (`:1`) + `SendGrid` مسجل بلا `AddSendGrid` + `Newtonsoft.Json` بلا استخدام → سطح اعتمادية ميت.
- **`Entities/MedStock.Data.csproj` شارد** (داخل `Entities/`, `net8.0-windows+UseWPF+SendGrid`) مقابل الجذري (`net8.0`) — الـ Solution يشير للجذري؛ الشارد قد يُبنى بالخطأ.
- **`Repositories/` فارغ** — نمط معلن بالمجلدات بلا كود → يحير الوكلاء.
- **`DI 10.0.2` + `Hosting 8.0.1`** — مزيج major غير متجانس (يعمل اليوم، خطر كسر مستقبلي).
- **لا `DispatcherUnhandledException` handler** — أي استثناء خارج `try` في VM يسقط WPF app.
- **`.gitignore` يستثني `appsettings.json` الجذري فقط** بينما الفعلي `MedStock.UI/appsettings.json` مُتتبع (يحمل `.\SQLEXPRESS`) — تسرب بيئة + التباس أي ملف يُقرأ (`App` يقرأ `%AppData%` لا الجذري).
