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
    public sealed class ItemsService : IItemsService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public ItemsService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<PagedResult<ItemListRow>> SearchAsync(ItemFilter filter, CancellationToken ct = default)
        {
            return await _db.ExecuteAsync<PagedResult<ItemListRow>>(async db =>
            {
                var query = db.Items.AsNoTracking()
                    .AsQueryable();

                // 1. الفلترة
                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var txt = filter.SearchText.Trim();
                    query = query.Where(x => x.ItemName.Contains(txt) || x.Sku.Contains(txt));
                }

                if (filter.IsActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == filter.IsActive.Value);
                }

                // 2. الحساب الكلي (قبل القص)
                var totalCount = await query.CountAsync(ct);

                // 3. جلب الصفحة
                var items = await query
                    .OrderBy(x => x.ItemName)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(x => new ItemListRow
                    {
                        ItemId = x.ItemId,
                        Name = x.ItemName,
                        Sku = x.Sku,
                        UnitOfMeasure = x.UnitOfMeasure,
                        ReorderLevel = x.ReorderLevel,
                        MinStock = x.MinStok, // لاحظ الاسم في قاعدة البيانات
                        IsActive = x.IsActive,
                        // هنا يتم حساب الكمية الكلية من الدفعات
                        TotalQuantity = x.Batches.Sum(b => b.CurrentQty)
                    })
                    .ToListAsync(ct);

                // 4. الإرجاع
                return new PagedResult<ItemListRow>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
                };
            }, ct);
        }

        public async Task<IReadOnlyList<ItemListRow>> GetItemsAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();

            var q = db.Items.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    x.ItemName.Contains(s) ||
                    x.Sku.Contains(s) ||
                    x.UnitOfMeasure.Contains(s));
            }

            return await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.ItemName)
                .Select(x => new ItemListRow
                {
                    ItemId = x.ItemId,
                    Name = x.ItemName,
                    Sku = x.Sku,
                    UnitOfMeasure = x.UnitOfMeasure,
                    ReorderLevel = x.ReorderLevel,
                    MinStock = x.MinStok,
                    IsActive = x.IsActive,
                    // حساب الكمية الكلية هنا أيضاً لضمان الاتساق
                    TotalQuantity = x.Batches.Sum(b => b.CurrentQty)
                })
                .ToListAsync(ct);
        }

        public async Task<ItemUpsertRequest> GetForEditAsync(int itemId, CancellationToken ct = default)
        {
            if (itemId <= 0) throw new InvalidOperationException("Invalid itemId.");

            await using var db = _factory.CreateDbContext();
            var item = await db.Items.AsNoTracking()
                .Where(x => x.ItemId == itemId)
                .Select(x => new ItemUpsertRequest
                {
                    ItemId = x.ItemId,
                    Name = x.ItemName,
                    Sku = x.Sku,
                    UnitOfMeasure = x.UnitOfMeasure,
                    ReorderLevel = x.ReorderLevel,
                    MinStock = x.MinStok,
                    Description = x.Description,
                    IsActive = x.IsActive,
                })
                .FirstOrDefaultAsync(ct);

            if (item is null) throw new InvalidOperationException("Item not found.");
            return item;
        }

        public Task<int> SaveAsync(ItemUpsertRequest request, int userId, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            // normalize
            request = new ItemUpsertRequest
            {
                ItemId = request.ItemId,
                Name = (request.Name ?? "").Trim(),
                Sku = (request.Sku ?? "").Trim().ToUpperInvariant(),
                UnitOfMeasure = (request.UnitOfMeasure ?? "").Trim(),
                ReorderLevel = request.ReorderLevel,
                MinStock = request.MinStock,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = request.IsActive
            };

            // required validations
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new InvalidOperationException("اسم المادة مطلوب.");

            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new InvalidOperationException("SKU مطلوب ولا يمكن تركه فارغاً.");

            if (string.IsNullOrWhiteSpace(request.UnitOfMeasure))
                throw new InvalidOperationException("وحدة القياس مطلوبة.");

            if (request.ReorderLevel < 0m)
                throw new InvalidOperationException("حد إعادة الطلب يجب أن يكون 0 أو أكثر.");

            if (request.MinStock.HasValue && request.MinStock.Value < 0m)
                throw new InvalidOperationException("حد التنبيه يجب أن يكون 0 أو أكثر.");

            return _db.ExecuteAsync<int>(async db =>
            {
                // حقن الـ User Audit
                db.SetAuditUser(userId);

                // التحقق من التكرار SKU
                var skuExists = await db.Items.AsNoTracking()
                    .AnyAsync(x => x.Sku.ToLower() == request.Sku.ToLower() && x.ItemId != (request.ItemId ?? 0), ct);

                if (skuExists) throw new InvalidOperationException($"SKU '{request.Sku}' مستخدم مسبقاً.");

                if (request.ItemId is null)
                {
                    var entity = new Item
                    {
                        ItemName = request.Name,
                        Sku = request.Sku,
                        UnitOfMeasure = request.UnitOfMeasure,
                        ReorderLevel = request.ReorderLevel,
                        MinStok = request.MinStock,
                        Description = request.Description,
                        IsActive = request.IsActive,
                        CreatedAt = DateTime.Now
                    };

                    db.Items.Add(entity);
                    await db.SaveChangesAsync(ct);
                    return entity.ItemId;
                }
                else
                {
                    var id = request.ItemId.Value;
                    var entity = await db.Items.FirstOrDefaultAsync(x => x.ItemId == id, ct);
                    if (entity is null) throw new InvalidOperationException("Item not found.");

                    entity.ItemName = request.Name;
                    entity.Sku = request.Sku;
                    entity.UnitOfMeasure = request.UnitOfMeasure;
                    entity.ReorderLevel = request.ReorderLevel;
                    entity.MinStok = request.MinStock;
                    entity.Description = request.Description;
                    entity.IsActive = request.IsActive;
                    entity.UpdatedAt = DateTime.Now;

                    await db.SaveChangesAsync(ct);
                    return entity.ItemId;
                }
            }, ct);
        }

        public Task DeleteAsync(int itemId, int userId, CancellationToken ct = default)
        {
            if (itemId <= 0) throw new InvalidOperationException("Invalid itemId.");
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            return _db.ExecuteAsync(async db =>
            {
                db.SetAuditUser(userId);

                var entity = await db.Items.FindAsync(new object[] { itemId }, ct);
                if (entity is null) throw new InvalidOperationException("المادة غير موجودة.");

                // التحقق من الموانع
                var hasBatches = await db.Batches.AsNoTracking().AnyAsync(b => b.ItemId == itemId, ct);
                if (hasBatches)
                    throw new InvalidOperationException("لا يمكن حذف المادة لأن لها سجلات مخزنية (Batches). يفضل استخدام 'التعطيل' بدلاً من الحذف.");

                var hasReqs = await db.RequisitionDetails.AsNoTracking().AnyAsync(r => r.ItemId == itemId, ct);
                if (hasReqs)
                    throw new InvalidOperationException("لا يمكن حذف المادة لأنها مرتبطة بطلبات صرف سابقة.");

                var hasStocktakes = await db.StocktakeDetails.AsNoTracking().AnyAsync(s => s.ItemId == itemId, ct);
                if (hasStocktakes)
                    throw new InvalidOperationException("لا يمكن حذف المادة لأنها مسجلة في عمليات جرد سابقة.");

                db.Items.Remove(entity);
                await db.SaveChangesAsync(ct);

            }, ct);
        }

        public Task SetActiveAsync(int itemId, bool isActive, int userId, CancellationToken ct = default)
        {
            if (itemId <= 0) throw new InvalidOperationException("Invalid itemId.");
            if (userId <= 0) throw new InvalidOperationException("Invalid userId.");

            return _db.ExecuteAsync(async db =>
            {
                var entity = await db.Items.FirstOrDefaultAsync(x => x.ItemId == itemId, ct);
                if (entity is null) throw new InvalidOperationException("Item not found.");

                entity.IsActive = isActive;
                entity.UpdatedAt = DateTime.Now;
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        public Task<IReadOnlyList<ItemBatchRow>> GetBatchesAsync(int itemId, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                return await db.Batches.AsNoTracking()
                    .Where(b => b.ItemId == itemId && b.CurrentQty > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => new ItemBatchRow
                    {
                        BatchId = b.BatchId,
                        BatchCode = b.BatchCode,
                        CurrentQty = b.CurrentQty,
                        ExpiryDate = b.ExpiryDate,
                        LocationCode = b.LocationCode,
                        UnitCost = b.UnitCost
                    })
                    .ToListAsync(ct) as IReadOnlyList<ItemBatchRow>;
            }, ct);
        }
    }
}