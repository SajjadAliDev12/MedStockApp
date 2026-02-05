using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MedStock.Data.Context
{
    // Partial class allows us to extend the context without modifying the auto-generated file
    public partial class HospitalInventoryDbContext
    {
        // خاصية لتمرير معرف المستخدم الحالي إلى السياق
        // Property to pass the current UserId to the context
        public int? CurrentUserId { get; set; }

        // دالة مساعدة لتعيين المستخدم بسهولة داخل الخدمات
        public void SetAuditUser(int userId)
        {
            CurrentUserId = userId;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // 1. التقاط التغييرات قبل الحفظ
            var auditEntries = OnBeforeSaveChanges();

            // 2. حفظ التغييرات الأساسية (ليتم توليد الـ IDs للعناصر الجديدة)
            var result = await base.SaveChangesAsync(cancellationToken);

            // 3. حفظ سجلات التغييرات (بعد الحصول على الـ IDs)
            await OnAfterSaveChanges(auditEntries);

            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // تجاهل سجلات الأوديت نفسها أو السجلات التي لم تتغير أو المنفصلة
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry);
                auditEntry.TableName = entry.Entity.GetType().Name; // أو استخدام Metadata للحصول على اسم الجدول الفعلي
                auditEntry.UserId = CurrentUserId;

                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    string propertyName = property.Metadata.Name;

                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditType = "Create";
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.AuditType = "Delete";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = "Update";
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }

            // تصفية التعديلات التي لم تغير شيئاً فعلياً
            foreach (var auditEntry in auditEntries.Where(a => a.AuditType == "Update").ToList())
            {
                if (auditEntry.ChangedColumns.Count == 0)
                    auditEntries.Remove(auditEntry);
            }

            return auditEntries;
        }

        private async Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            foreach (var auditEntry in auditEntries)
            {
                // للعناصر الجديدة، نحصل على الـ ID بعد الحفظ
                foreach (var prop in auditEntry.TemporaryProperties)
                {
                    if (prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    else
                    {
                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                // تحويل AuditEntry المساعد إلى AuditLog Entity وحفظه
                AuditLogs.Add(auditEntry.ToAuditLog());
            }

            // حفظ سجلات الأوديت
            await base.SaveChangesAsync();
        }
    }

    // كلاس مساعد مؤقت لتجميع البيانات
    public class AuditEntry
    {
        public EntityEntry Entry { get; }
        public int? UserId { get; set; }
        public string TableName { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();
        public List<string> ChangedColumns { get; } = new List<string>();
        public string AuditType { get; set; }

        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public AuditLog ToAuditLog()
        {
            var audit = new AuditLog();
            audit.UserId = UserId;
            audit.ActionType = AuditType;
            audit.EntityName = TableName;
            audit.OccurredAt = DateTime.Now; // توحيد التوقيت

            // تكوين المفتاح الأساسي كـ String
            audit.EntityId = JsonSerializer.Serialize(KeyValues).Trim('{', '}').Replace("\"", "");
            if (audit.EntityId.Length > 50) audit.EntityId = audit.EntityId.Substring(0, 50); // Truncate if too long

            // إنشاء الملخص
            audit.Summary = $"{AuditType} {TableName} ({audit.EntityId})";
            if (ChangedColumns.Count > 0)
                audit.Summary += $" changed: {string.Join(", ", ChangedColumns)}";

            // تحويل التفاصيل إلى JSON
            // نستخدم NewValues للإضافة، Old+New للتعديل
            var details = new Dictionary<string, object>();
            if (AuditType == "Create")
                details["New"] = NewValues;
            else if (AuditType == "Delete")
                details["Old"] = OldValues;
            else
            {
                details["Old"] = OldValues;
                details["New"] = NewValues;
            }

            audit.DetailsJson = JsonSerializer.Serialize(details);

            return audit;
        }
    }
}