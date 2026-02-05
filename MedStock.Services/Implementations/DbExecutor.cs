using System;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Services.Implementations
{
    public sealed class DbExecutor
    {
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public DbExecutor(Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task ExecuteAsync(
            Func<HospitalInventoryDbContext, Task> action,
            CancellationToken ct = default)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            await using var db = _factory.CreateDbContext();

            // Required when EnableRetryOnFailure is enabled
            var strategy = db.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync(ct);

                    await action(db);

                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(EfErrorTranslator.ToUserMessage(ex), ex);
            }
        }

        public async Task<T> ExecuteAsync<T>(
            Func<HospitalInventoryDbContext, Task<T>> action,
            CancellationToken ct = default)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));

            await using var db = _factory.CreateDbContext();
            var strategy = db.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync(ct);

                    var result = await action(db);

                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    return result;
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(EfErrorTranslator.ToUserMessage(ex), ex);
            }
        }
    }
}
