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
    public sealed class SuppliersService : ISuppliersService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public SuppliersService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<SupplierListRow>> GetListAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            var q = db.Suppliers.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.SupplierName.Contains(s) || x.Phone.Contains(s));
            }

            return await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.SupplierName)
                .Select(x => new SupplierListRow
                {
                    SupplierId = x.SupplierId,
                    Name = x.SupplierName,
                    Phone = x.Phone,
                    Email = x.Email,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);
        }

        public Task<int> SaveAsync(SupplierUpsertRequest request, int userId, CancellationToken ct = default)
        {
            Guard.NotNullOrWhiteSpace(request.Name, "اسم المورد");

            return _db.ExecuteAsync(async db =>
            {
                if (request.SupplierId.HasValue)
                {
                    // Update
                    var entity = await db.Suppliers.FindAsync(new object[] { request.SupplierId.Value }, ct);
                    if (entity == null) throw new InvalidOperationException("المورد غير موجود.");

                    entity.SupplierName = request.Name;
                    entity.Phone = request.Phone;
                    entity.Email = request.Email;
                    entity.Address = request.Address;
                    entity.IsActive = request.IsActive;

                    return entity.SupplierId;
                }
                else
                {
                    // Insert
                    var entity = new Supplier
                    {
                        SupplierName = request.Name,
                        Phone = request.Phone,
                        Email = request.Email,
                        Address = request.Address,
                        IsActive = request.IsActive,
                        CreatedAt = DateTime.Now
                    };
                    db.Suppliers.Add(entity);
                    await db.SaveChangesAsync(ct); // Save to get ID
                    return entity.SupplierId;
                }
            }, ct);
        }

        public Task ToggleActiveAsync(int supplierId, int userId, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                var entity = await db.Suppliers.FindAsync(new object[] { supplierId }, ct);
                if (entity != null)
                {
                    entity.IsActive = !entity.IsActive;
                }
            }, ct);
        }

        public async Task<IReadOnlyList<IdNameRow>> GetLookupAsync(CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            return await db.Suppliers.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SupplierName)
                .Select(x => new IdNameRow { Id = x.SupplierId, Name = x.SupplierName, IsActive = true })
                .ToListAsync(ct);
        }
    }
}