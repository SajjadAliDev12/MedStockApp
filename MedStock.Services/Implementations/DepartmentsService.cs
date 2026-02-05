using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Data.Context;
using MedStock.Data.Entities;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Services.Implementations
{
    public sealed class DepartmentsService : IDepartmentsService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public DepartmentsService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<DepartmentListRow>> GetAsync(string? search, bool includeInactive = true, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();

            IQueryable<Department> q = db.Departments.AsNoTracking();

            if (!includeInactive)
                q = q.Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    x.DepartmentName.Contains(s) ||
                    (x.DepartmentCode != null && x.DepartmentCode.Contains(s)) ||
                    (x.Notes != null && x.Notes.Contains(s)));
            }

            return await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.DepartmentName)
                .Select(x => new DepartmentListRow
                {
                    DepartmentId = x.DepartmentId,
                    Code = x.DepartmentCode,
                    Name = x.DepartmentName,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<DepartmentUpsertRequest> GetForEditAsync(int departmentId, CancellationToken ct = default)
        {
            if (departmentId <= 0) throw new InvalidOperationException("Invalid departmentId.");

            await using var db = _factory.CreateDbContext();

            var dto = await db.Departments.AsNoTracking()
                .Where(x => x.DepartmentId == departmentId)
                .Select(x => new DepartmentUpsertRequest
                {
                    DepartmentId = x.DepartmentId,
                    Code = x.DepartmentCode,
                    Name = x.DepartmentName,
                    Notes = x.Notes,
                    IsActive = x.IsActive
                })
                .FirstOrDefaultAsync(ct);

            if (dto is null) throw new InvalidOperationException("Department not found.");

            return dto;
        }

        public Task<int> SaveAsync(DepartmentUpsertRequest req, int userId, CancellationToken ct = default)
        {
            if (req is null) throw new ArgumentNullException(nameof(req));
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            // normalize (نفس أسلوبك في Items/Categories)
            var name = (req.Name ?? "").Trim();
            var code = string.IsNullOrWhiteSpace(req.Code) ? null : req.Code!.Trim().ToUpperInvariant();
            var notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes!.Trim();

            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("اسم القسم مطلوب.");

            // ملاحظة: في DB عندك DepartmentCode يسمح NULL لكنه عليه UNIQUE
            // SQL Server غالباً يسمح NULL واحد فقط تحت UNIQUE.
            // لذلك هذا الكود سيمنع التكرار إذا code != null، لكنه لا يحل مشكلة NULL المتكرر على مستوى DB.
            // (أفضل حل: filtered unique index في SQL)

            return _db.ExecuteAsync<int>(async db =>
            {
                db.SetAuditUser(userId);

                // Unique: DepartmentName
                var nameExists = await db.Departments.AsNoTracking()
                    .AnyAsync(x => x.DepartmentName == name && x.DepartmentId != (req.DepartmentId ?? 0), ct);

                if (nameExists)
                    throw new InvalidOperationException("اسم القسم موجود مسبقاً.");

                // Unique: DepartmentCode (إذا مو null)
                if (code != null)
                {
                    var codeExists = await db.Departments.AsNoTracking()
                        .AnyAsync(x => x.DepartmentCode != null &&
                                       x.DepartmentCode.ToUpper() == code.ToUpper() &&
                                       x.DepartmentId != (req.DepartmentId ?? 0), ct);

                    if (codeExists)
                        throw new InvalidOperationException($"رمز القسم '{code}' موجود مسبقاً.");
                }

                if (req.DepartmentId is null)
                {
                    var entity = new Department
                    {
                        DepartmentCode = code,
                        DepartmentName = name,
                        Notes = notes,
                        IsActive = req.IsActive,
                        CreatedAt = DateTime.Now
                    };

                    db.Departments.Add(entity);
                    return entity.DepartmentId;
                }
                else
                {
                    var entity = await db.Departments
                        .FirstOrDefaultAsync(x => x.DepartmentId == req.DepartmentId.Value, ct);

                    if (entity is null) throw new InvalidOperationException("Department not found.");

                    entity.DepartmentCode = code;
                    entity.DepartmentName = name;
                    entity.Notes = notes;
                    entity.IsActive = req.IsActive;

                    return entity.DepartmentId;
                }
            }, ct);
        }

        public Task SetActiveAsync(int departmentId, bool isActive, int userId, CancellationToken ct = default)
        {
            if (departmentId <= 0) throw new InvalidOperationException("Invalid departmentId.");
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            return _db.ExecuteAsync(async db =>
            {
                db.SetAuditUser(userId);

                var entity = await db.Departments
                    .FirstOrDefaultAsync(x => x.DepartmentId == departmentId, ct);

                if (entity is null) throw new InvalidOperationException("Department not found.");

                entity.IsActive = isActive;
            }, ct);
        }
    }
}
