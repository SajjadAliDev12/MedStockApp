# Live DB snapshot — HospitalInventoryDb (`.\SQLEXPRESS`)

Verified **2026-09-23** via `sqlcmd -S ".\SQLEXPRESS" -d HospitalInventoryDb -E`.
Connection string source: `MedStock.UI/appsettings.json`
(`Server=.\SQLEXPRESS;Database=HospitalInventoryDb;Trusted_Connection=True;…`).

This folder is documentation only — the database itself is Database-First
and lives outside the repo. Any schema change = external SQL script,
never `EnsureCreated` / `Migrate()`.

## Triggers (5, all active)

| # | Trigger | Table | Event | Purpose |
|---|---------|-------|-------|---------|
| 1 | `trg_RFL_UpdateFulfilledQty` | `RequisitionDetails` (`deleted` join) | AFTER (fulfillment link) | Keeps `FulfilledQty` in sync with fulfillment links |
| 2 | `trg_RFL_ValidateLink` | `RequisitionFulfillmentLinks` | AFTER INSERT | Rejects issue of an item different from the requested item |
| 3 | `trg_Transactions_ValidateReasonScope` | `Transactions` | AFTER (insert/update) | Validates `ReasonCode` scope (IN vs OUT) |
| 4 | `TR_TransactionDetails_AfterInsert` | `Transaction_Details` | AFTER INSERT, UPDATE, DELETE | Reverses old-row effect, applies new-row effect on `Batches` (ledger recompute via `iAgg`) |
| 5 | `trg_TransactionDetails_StockLedger` | `Transaction_Details` | AFTER INSERT | `Batches.CurrentQty += Qty` for `TransactionType='I'`, `-= Qty` for `'O'` |

> Code must NEVER update `Batches.CurrentQty` manually — the balance moves
> only through these triggers (see `AGENTS.md`).

Full definitions: `db/triggers/<name>.sql`, extracted with
`SELECT OBJECT_DEFINITION(OBJECT_ID('dbo.<name>'))`.
Extraction flags: `sqlcmd -y 0 -w 8192 -f 65001` (`-f 65001` UTF-8 is
required — without it Arabic comments come out as `????`; `-y 0` is
required — without it definitions truncate ~mid-statement; sqlcmd header
lines stripped). Completeness check: file char count == server
`LEN(OBJECT_DEFINITION(...))` (`2115 / 924 / 795 / 864 / 834`).

## Files

- `db/schema.sql` — نسخة السكيما الكاملة (18 جدول + Indexes + Defaults + FKs + CHECKs) المرفقة من المالك 2026-09-23. **بلا Triggers وبلا بيانات** (تحقق: `CREATE TRIGGER` صفر، `INSERT` صفر).
- `db/triggers/*.sql` — التعريفات الحية الخمسة (المصدر الوحيد للـ Triggers).
- `db/seed_roles.sql` — بذرة الأدوار (قابلة لإعادة التشغيل، نُفذت على DB الحية 2026-09-23).

## TransactionReasons (12 codes)

`ADJ_IN, ADJ_OUT, DAMAGE, DISPENSE, DONATION, EXPIRED, INITIAL, PURCHASE, REQ_FULFILL, RETURN_IN, STOCKTAKE_OUT, STOCKTAKEIN`
(`SELECT ReasonCode FROM dbo.TransactionReasons ORDER BY ReasonCode`).

## Users / Roles

- `dbo.Users` = **3** rows (موجودون مسبقاً، بلا أدوار مسندة).
- `dbo.Roles` = **5** rows (seed 2026-09-23): `Admin`, `StoreManager`, `Storekeeper`, `Auditor`, `Department` — التفاصيل والصلاحيات المقصودة في `db/seed_roles.sql`.
- الخطوة التالية اليدوية: افتح شاشة **المستخدمين والصلاحيات** في التطبيق وأسند دوراً لكل مستخدم من الثلاثة (الشاشة تشترط دوراً واحداً على الأقل). مستخدم admin جديد يُنشأ من نفس الشاشة (كلمة المرور تُخزن PBKDF2 — لا يمكن إنشاؤه بـ SQL).
