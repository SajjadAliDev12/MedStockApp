using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MedStock.Data.Context;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;

namespace MedStock.Services.Implementations
{
    public sealed class AlertsService : IAlertsService
    {
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public AlertsService(Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<MinStockAlertRow>> GetMinStockAlertsAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();

            var stock = db.Batches.AsNoTracking()
                .GroupBy(b => b.ItemId)
                .Select(g => new { ItemId = g.Key, Qty = g.Sum(x => x.CurrentQty) });

            var q =
                from i in db.Items.AsNoTracking()
                join s in stock on i.ItemId equals s.ItemId into sj
                from s in sj.DefaultIfEmpty()
                where i.IsActive && i.MinStok != null
                let current = (decimal?)s.Qty ?? 0m
                where current <= i.MinStok.Value
                select new MinStockAlertRow
                {
                    ItemId = i.ItemId,
                    Name = i.ItemName,
                    Sku = i.Sku,
                    MinStock = i.MinStok,
                    CurrentStock = current
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                q = q.Where(x => x.Name.Contains(term) || (x.Sku != null && x.Sku.Contains(term)));
            }

            return await q
                .OrderBy(x => x.CurrentStock)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);
        }
    }
}
