-- =============================================================
-- MedStock: Seed Roles (Hospital Inventory context)
-- Date: 2026-09-23 | DB: HospitalInventoryDb | Table: dbo.Roles
-- =============================================================
-- السياق (من الكود الفعلي، لا تخمين):
--   * أسماء الأدوار حرة تماماً في التطبيق (UserService يجلب RoleName
--     كنص، وUsersView يعرض أي دور في DB ويشترط دوراً واحداً على الأقل).
--   * لا يوجد فحص صلاحيات في الكود بعد (RBAC مرحلة M3) — هذه البذرة
--     تجهز الأسماء المستقرة التي سيُبنى عليها التقييد لاحقاً.
--   * مجالات التطبيق: إدخال/صرف مخزني، طلبات أقسام (Draft→Fulfilled)،
--     جرد (Draft→Posted)، بيانات أساسية، مستخدمون، تقارير وتدقيق.
-- آمنة وقابلة لإعادة التشغيل (IF NOT EXISTS لكل دور).
-- =============================================================
USE [HospitalInventoryDb];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Admin')
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'Admin', N'مدير النظام: كل الصلاحيات (المستخدمون والأدوار + كل العمليات والتقارير والجرد)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'StoreManager')
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'StoreManager', N'مدير المخزن: اعتماد/رفض الطلبات + ترحيل الجرد + التقارير + البيانات الأساسية (بدون إدارة المستخدمين)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Storekeeper')
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'Storekeeper', N'أمين المخزن: إدخال وصرف + إنشاء وإرسال الطلبات + عدّ الجرد (بدون اعتماد أو ترحيل)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Auditor')
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'Auditor', N'مدقق: عرض وقراءة فقط (التقارير + سجل التدقيق + بطاقة الصنف)');
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Department')
    INSERT INTO dbo.Roles (RoleName, Description)
    VALUES (N'Department', N'القسم الطالب: إنشاء طلبات الصرف ومتابعتها فقط');
GO

-- تحقق:
SELECT RoleId, RoleName, Description FROM dbo.Roles ORDER BY RoleId;
GO
