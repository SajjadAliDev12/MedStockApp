# TODO — الأجزاء غير المكتملة (مصنفة من الفحص الفعلي)

> المعيار: `معيار النجاح` لكل بند = شرط قابل للتحقق قبل حذفه من القائمة وتحديث `PROJECT_MAP.md [ORPHANS & PENDING]`.

## P0 — كسر وظيفي (يوقف ميزة)

- [x] **T01 — `IInventoryService.GetReasonsAsync` غير منفذة** — نُفذت 2026-09-23 (استعلام `TransactionReasons` النشطة مع فلتر `Scope I/O/B`، مرتبة ﺑ`ReasonName`). تحقق: `dotnet build` 0 Errors + استعلامات النطاق I/O تُرجع 6+6 صفوف على DB الحية.
- [x] **T02 — روابط ميتة في `RequisitionDetailsView.xaml`** — أُصلحت 2026-09-23: أُضيف `DetailsTitle` + `DeptLabel` + `SaveHeaderCommand→UpdateHeaderAsync` للـ VM، وأُضيف `CommandParameter="{Binding SelectedLine.RequisitionDetailId}"` لزر الحذف.
- [x] **T03 — سكربتات DB والـ Triggers** — اكتمل التوثيق 2026-09-23: `db/schema.sql` (نسخة المالك: 18 جدول، بلا Triggers/بيانات — متحقق) + `db/triggers/*.sql` (5 تعريفات حية مستخرجة) + `db/seed_roles.sql` (نُفذ على DB الحية: 5 أدوار). المتبقي يدوي: إسناد أدوار للمستخدمين الثلاثة من شاشة المستخدمين (لا يمكن إنشاء كلمات مرور بـ SQL — PBKDF2).
- [x] **T04 — `SuppliersViewModel.Selected` يمسح العنوان** — أُصلح 2026-09-23: أُضيف `Address` لـ `SupplierListRow` + تعبئته في `SuppliersService.GetListAsync` + `AddressText = _selected.Address ?? ""` (المسح المتبقي في `StartNew()` مقصود).

## P1 — عيوب UI/ربط (تعمل جزئياً)

- [x] **T05 — `MinStockAlertsView.xaml` صف خارج النطاق** — أُصلح 2026-09-23 (`Grid.Row="6"` → `"3"`).
- [x] **T06 — `DiffColor string → Foreground Brush`** — أُصلح 2026-09-23 (أصبح `Brush`: `Brushes.Crimson/Green/Black`).
- [x] **T07 — `ExpiryReportView` تحقق** — مغلق 2026-09-23: `ExpiryReportView.xaml.cs` ينادي `LoadDataAsync()` في `Loaded` (موثق بالقراءة؛ بقي فحص بصري دقيقة عند التشغيل).
- [x] **T08 — `MainWindow.xaml` صف رأسي ميت** — أُصلح 2026-09-23 (حُذف `RowDefinitions` الزائد، `ContentControl` يملأ).
- [x] **T09 — `DashboardViewModel.InitAsync` تحقق** — مغلق 2026-09-23: `DashboardView.xaml.cs` ينادي `InitAsync()` في `Loaded` (العدادات الثلاثة ستتحمل؛ بقي فحص بصري).

## P2 — نظافة/ديون تقنية (لا تكسر التشغيل)

- [x] **T10 — ملفات يتيمة** — نُظف 2026-09-23: حُذف `Class1.cs` + `Entities/MedStock.Data.csproj` الشارد + `Strings.Designer.cs` الجذري + `StatusToVisibilityConverter.cs` + `using Azure.Core` (كلها `grep` صفر مراجع). أُزيلت بقايا `Strings.resx` الجذري من `.csproj`. بقي `Data/Repositories/` مجلداً فارغاً عمداً (لا كود). `dotnet build` 0 Errors.
- [x] **T11 — تسجيل مزدوج `ConfigFilePointer`** — أُصلح 2026-09-23: تسجيل واحد (`new ViewModels.ConfigFilePointer`) + حُذف الـ factory والـ class المكرر.
- [x] **T12 — خلط `DI 10.0.2` مع `Hosting 8.0.1`** — مقبول وموثق 2026-09-23: يعمل (بناء أخضر + 23 اختباراً). Downgrade بلا فائدة وظيفية؛ أُبقي عمداً مع هذه الملاحظة.
- [x] **T13 — `SendGrid` بلا استخدام** — حُلّ 2026-09-23: أصبح مستخدماً فعلياً (`IEmailService/EmailService` + زر ملخص التنبيهات في Dashboard + زر اختبار بريد في DatabaseSetup). `Newtonsoft.Json` بقي معلناً بلا استخدام — دين واحد صغير موثق للتنظيف القادم.
- [x] **T14 — `Item.MinStok` typo** — أُصلح 2026-09-23: العمود الحي كان `MinStock` أصلاً (السcaffold كان متخلفاً)؛ وُحّد الكود (`Item.cs` + Fluent + `AlertsService/ItemsService`) + `db/migrations/V1_minStok_rename.sql` (idempotent، no-op آمن على DB الحية).
- [x] **T15 — `TransactionDetail.StocktakeDetailId` بلا navigation** — موثق 2026-09-23: عمود أرشيف legacy يُقرأ/يُكتب كقيمة فقط؛ تسويات الجرد تمر عبر `BatchId` والـ Triggers، لا أثر وظيفي.
- [x] **T16 — `async void`** — عُولج 2026-09-23: معالجات عامة (`App.Logging.cs`: `DispatcherUnhandled` + `AppDomain` + `UnobservedTask` → `FileLogger`، لا سقوط صامت) + كل مسارات VM تعرض `StatusMessage`.
- [x] **T17 — توحيد الزمن** — سياسة موثقة 2026-09-23: **التوقيت المحلي للخادم** (`DateTime.Now` في كل الكتابة؛ أُزيلت `UtcNow` الوحيدة في RFL). `sysutcdatetime()` تبقى defaults احتياطية في DB فقط.
- [x] **T18 — `Supplier.CreatedAt default getdate()`** — وُحّد 2026-09-23 على `sysutcdatetime()` (default constraint أُعيد إنشاؤه على DB الحية؛ يؤثر على الصفوف الجديدة فقط).

## P3 — لاحقاً (بعد P0) — كلها مغلقة 2026-09-23

- [x] **T19** — `AuditLog.IpAddress/MachineName` تُملأ الآن (`Auditing.ToAuditLog`: `Environment.MachineName` + أول IPv4 غير loopback).
- [x] **T20** — `LastLoginAt` يُحدَّث عند كل دخول ناجح (`UserService`) + قفل بعد 5 محاولات فاشلة 15 دقيقة (`FailedAttempts/LockedUntil` + `db/migrations/V2_user_lockout.sql` منفذ على الحية).
- [x] **T21** — `_isPasswordMode` الميت حُذف + شاشة المستخدمين أصبحت `PasswordBox` (لا ربط، `CommandParameter` كـ Login).
- [x] **T22** — `CreateDraft` يحمل `SetAuditUser` + رقم فريد مضمون + فائض `ADJ_IN` + `BatchCode` فريد + رسالة كسرية (انظر P0).
- [x] **T23** — `PostAsync` ذري الآن (كتلة `DbExecutor` واحدة: تسويات + ترحيل معاً).
