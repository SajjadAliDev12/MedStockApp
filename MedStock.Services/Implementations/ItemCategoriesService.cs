using System;
using System.Collections.Generic;
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
    public sealed class ItemCategoriesService : IItemCategoriesService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public ItemCategoriesService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<IdNameRow>> GetItemsAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();

            var q = db.Items.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.ItemName.Contains(s) || (x.Sku != null && x.Sku.Contains(s)));
            }

            return await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.ItemName)
                .Select(x => new IdNameRow
                {
                    Id = x.ItemId,
                    Name = x.ItemName,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<IdNameRow>> GetCategoriesAsync(CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            return await db.Categories.AsNoTracking()
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.CategoryName)
                .Select(x => new IdNameRow
                {
                    Id = x.CategoryId,
                    Name = x.CategoryName,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public async Task<IReadOnlySet<int>> GetAssignedCategoryIdsAsync(int itemId, CancellationToken ct = default)
        {
            if (itemId <= 0) throw new InvalidOperationException("Invalid itemId.");

            await using var db = _factory.CreateDbContext();

            var ids = await db.ItemCategories.AsNoTracking()
                .Where(x => x.ItemId == itemId)
                .Select(x => x.CategoryId)
                .ToListAsync(ct);

            return ids.ToHashSet();
        }

        public Task SetAssignedCategoriesAsync(int itemId, IReadOnlyCollection<int> categoryIds, int userId, CancellationToken ct = default)
        {
            if (itemId <= 0) throw new InvalidOperationException("Invalid itemId.");
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            categoryIds ??= Array.Empty<int>();
            foreach (var id in categoryIds)
                if (id <= 0) throw new InvalidOperationException("Invalid categoryId in list.");

            return _db.ExecuteAsync(async db =>
            {
                var itemExists = await db.Items.AsNoTracking().AnyAsync(x => x.ItemId == itemId, ct);
                if (!itemExists) throw new InvalidOperationException("المادة غير موجودة.");

                // Remove old links
                var oldLinks = await db.ItemCategories.Where(x => x.ItemId == itemId).ToListAsync(ct);
                db.ItemCategories.RemoveRange(oldLinks);

                // Add new links (distinct)
                foreach (var catId in categoryIds.Distinct())
                {
                    db.ItemCategories.Add(new ItemCategory
                    {
                        ItemId = itemId,
                        CategoryId = catId,
                        CreatedAt = DateTime.Now
                    });
                }
            }, ct);
        }
    }
}
