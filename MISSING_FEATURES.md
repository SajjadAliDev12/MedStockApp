# MISSING_FEATURES — ما يفتقده MedStock مقابل أنظمة مخزون المستشفيات/المذاخر المشابهة

> ليست أوامر تنفيذ (No Feature Creep) — قائمة مرجعية للإضافة لاحقاً بموافقة صريحة. كل بند مفقود فعلاً من الريبو (grep/قراءة مباشرة).

## 1. الحوكمة والجودة (الأعلى أثراً)

- **Tests**: صفر مشاريع اختبار (`*.Tests`/`xunit/nunit` غير موجودة). المشابهة تملك Unit (Services) + Integration (EF InMemory/Sqlite). المطلوب لاحقاً: `MedStock.Tests` يغطي `StockIn/Out FEFO`، حالات `Requisition`، `Stocktake Post`.
- **CI/CD**: لا `.github/workflows`، لا lint/format config. المطلوب: `build+test` على PR.
- **Logging framework**: لا `Serilog/NLog` — فقط `Debug.WriteLine` + نصوص `StatusMessage`. المطلوب (حسب بروتوكول Safe Logging): ملف async بسيط (`logs/medstock-.log`) بمستويات `Info/Warn/Error` فقط، غير حظري، يُستدعى من `DbExecutor.catch` و`Dashboard catch`.
- **Migrations/DB versioning**: لا `Migrations/` ولا `db/*.sql`. المشابهة تملك `migrations + seed`. المطلوب: `db/schema.sql + triggers.sql + seed.sql` (الأسباب الـ 12 + أدوار + admin أولي) — يعالج T03 مباشرة.

## 2. الأمان والصلاحيات

- **RBAC فعلي**: `Roles` موجودة لكن لا فحص `IsInRole` ولا إخفاء قوائم.
  - **قرار المالك (2026-09-23): نموذج Hospital مفصّل (Admin/Storekeeper/Pharmacist/Auditor/Department).** يُصمم لاحقاً بموافقة: `SessionUser.IsInRole()` + `CanExecute` + `Visibility` + منع `Approve` لغير المخول.
- **Password policy + lockout**: لا سياسة طول/تعقيد مرصودة، لا قفل بعد محاولات، لا `LastLoginAt` تحديث، لا تغيير كلمة المرور الذاتي. المشابهة تملكها.
- **Audit completeness**: `IpAddress/MachineName` أعمدة بلا تعبئة (`Auditing.cs` لا يلتقطها)؛ لا شاشة تفاصيل `DetailsJson` موسعة؛ لا تصدير سجل التدقيق.

## 3. عمليات المخزن اليومية

- **Purchase orders / Goods receipt مكتمل**: يوجد `StockIn` يدوي فقط؛ لا `PurchaseOrder (طلب شراء → استلام جزئي → فاتورة مورد)` ولا ربط `Supplier invoice` (فقط `SupplierRef` نصي في السطر).
- **Barcode/SKU lookup متقدم**: بحث نصي فقط (`length>=2`)؛ لا ماسح باركود، لا طباعة ملصقات `Batch/Expiry`.
- **Locations/Warehouses**: `LocationCode` نص حر في `Batch` فقط؛ لا `Warehouse/Zone/Shelf` ككيانات ولا تحويل بين مخازن.
- **Expiry workflows**: تقرير عرض فقط؛ لا `FEFO auto-suggest` في UI (موجود في Service فقط)، لا تنبيه بريد/إشعار، لا `Write-off` معتمد بسير (`EXPIRED/DAMAGE` رموز بلا شاشة إتلاف مخصصة — تُستخدم عبر `StockOut` العام).
- **Stocktake بالباركود/العد الجزئي**: الجرد لقطة شاملة لكل `Items.IsActive` دفعة واحدة (قد تكون آلاف السطور)؛ لا جرد جزئي/دوري (ABC)، لا عدّ عبر ملف/جهاز، لا فروق بقيم مالية (التسوية `UnitCost=0`).

## 4. التقارير والطباعة

- **تصدير/طباعة**: CSV واحد فقط (`ConsumptionReportViewModel.Export`)؛ لا PDF/Excel للفواتير والجرد وبطاقة الصنف، لا معاينة طباعة، لا ترويسة مستشفى/شعار.
- **لوحة قيادة**: 3 عدادات فقط (`LowStock/Submitted/Expiring90`)؛ لا رسوم (استهلاك شهري، أعلى أصناف، مخزون راكد/منتهي القيمة)، لا تنبيهات `ReorderLevel` مقابل `MinStok` (يوجد عمودان بلا تمييز واضح في UI).
- **تقارير مفقودة**: حركة مورد، مشتريات فترة، جرد مقارن (قبل/بعد)، أداء الأقسام، قيمة المخزون (`Qty×UnitCost` — البيانات موجودة لكن لا تقرير قيمة).

## 5. قابلية التشغيل والصيانة

- **Backup/Restore**: لا شاشة نسخ احتياطي SQL (المشابهة المحلية تملك زر `.bak`).
- **User activity/lock**: لا جلسات متعددة/مهلة خمول (`SessionContext` دائم حتى Logout)، لا `Change password` ذاتي، لا `Forgot password`.
- **Notifications**: `SendGrid` معلن بلا تفعيل.
  - **قرار المالك (2026-09-23): تفعيل لاحق (تنبيه MinStock/Expiry/PendingApproval بريد/داخلي).** ممنوع الحذف؛ يُصمم بعد M1.
- **Multi-branch/Activity**: `LanguageService(ActivityType)` يبدل labelين فقط (`Hospital` مقابل غيره)؛ لا دعم فروع/مخازن متعددة فعلي، ولا عملات/ضرائب (تجاري).
- **Archiving**: لا أرشفة حركات قديمة؛ `AuditLogs` ينمو بلا Retention policy.
- **Accessibility**: لا `AutomationProperties`، لا اختصارات لوحة مفاتيح، لا وضع تباين عالٍ (ثيم واحد Teal).

## 6. مقترح Milestones — الحالة 2026-09-23

- **M1 — التثبيت**: DONE تقنياً (T03 + T01 + T02 + Triggers موثقة + أدوار). الباقي: تشغيل قبول يدوي واحد (`StockIn→Approve→Fulfill→StockCard`).
- **M2 — الموثوقية**: DONE (Logging ملف + P0/P1 + بناء 0 Errors + 23 اختباراً + CI).
- **M3 — الحوكمة**: DONE تقنياً (RBAC + قفل + سياسة + Audit IP/Machine). الباقي: إسناد الأدوار للمستخدمين الثلاثة.
- **M4 — التقارير**: DONE تقنياً (قيمة المخزون + CSV×4 + طباعة + رسوم + digest بريد). الباقي: تعبئة `Email:ApiKey` + طباعة تجريبية.
