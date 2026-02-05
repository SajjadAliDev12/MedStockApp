using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedStock.Data.Context;
using MedStock.Data.Entities;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.Services.Implementations
{
    public sealed class CategoriesService : ICategoriesService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public CategoriesService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<CategoryListRow>> GetAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();

            IQueryable<Category> q = db.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.CategoryName.Contains(s));
            }

            return await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.CategoryName)
                .Select(x => new CategoryListRow
                {
                    CategoryId = x.CategoryId,
                    Name = x.CategoryName,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public Task<int> SaveAsync(CategoryUpsertRequest req, int userId, CancellationToken ct = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("اسم التصنيف مطلوب.");

            return _db.ExecuteAsync<int>(async db =>
            {
                
                var exists = await db.Categories.AsNoTracking()
                    .AnyAsync(x => x.CategoryName == name && x.CategoryId != (req.CategoryId ?? 0), ct);
                if (exists) throw new InvalidOperationException("اسم التصنيف موجود مسبقاً.");

                if (req.CategoryId is null)
                {
                    var entity = new Category
                    {
                        CategoryName = name,
                        IsActive = req.IsActive,
                        CreatedAt = DateTime.Now
                    };
                    db.Categories.Add(entity);
                    return entity.CategoryId;
                }
                else
                {
                    var entity = await db.Categories.FirstOrDefaultAsync(x => x.CategoryId == req.CategoryId.Value, ct);
                    if (entity == null) throw new InvalidOperationException("التصنيف غير موجود.");

                    entity.CategoryName = name;
                    entity.IsActive = req.IsActive;
                    entity.UpdatedAt = DateTime.Now;
                    return entity.CategoryId;
                }
            }, ct);
        }

        public Task SetActiveAsync(int categoryId, bool isActive, int userId, CancellationToken ct = default)
        {
            if (categoryId <= 0) throw new InvalidOperationException("Invalid CategoryId.");
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            return _db.ExecuteAsync(async db =>
            {
                var entity = await db.Categories.FirstOrDefaultAsync(x => x.CategoryId == categoryId, ct);
                if (entity == null) throw new InvalidOperationException("التصنيف غير موجود.");

                entity.IsActive = isActive;
                entity.UpdatedAt = DateTime.Now;
            }, ct);
        }
    }
}
