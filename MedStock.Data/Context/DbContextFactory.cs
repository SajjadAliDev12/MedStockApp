using System;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Data.Context
{
    public interface IDbContextFactory<out TContext> where TContext : DbContext
    {
        TContext CreateDbContext();
    }

    public sealed class HospitalDbContextFactory : IDbContextFactory<HospitalInventoryDbContext>
    {
        private readonly DbContextOptions<HospitalInventoryDbContext> _options;

        public HospitalDbContextFactory(DbContextOptions<HospitalInventoryDbContext> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public HospitalInventoryDbContext CreateDbContext()
        {
            var ctx = new HospitalInventoryDbContext(_options);
            ctx.ConfigureDefaults();
            return ctx;
        }
    }
}
