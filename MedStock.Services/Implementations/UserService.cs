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
    public sealed class UserService : IUserService
    {
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public UserService(Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<SessionUser> AuthenticateAsync(string username, string password, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("Username is required.");
            if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Password is required.");

            await using var db = _factory.CreateDbContext();

            // English: load user + roles in one go (adjust navigation names if scaffold differs)
            var user = await db.Users
                .AsNoTracking()
                .Where(u => u.Username == username && u.IsActive)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.DisplayName,
                    u.PasswordSalt,
                    u.PasswordHash,
                    u.FailedAttempts,
                    u.LockedUntil,
                    Roles = db.UserRoles
                        .Where(ur => ur.UserId == u.UserId)
                        .Join(db.Roles, ur => ur.RoleId, r => r.RoleId, (ur, r) => r.RoleName)
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (user is null) throw new InvalidOperationException("Invalid username or password.");

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
                throw new InvalidOperationException($"الحساب مقفل حتى {user.LockedUntil.Value:HH:mm dd/MM/yyyy}. حاول لاحقاً.");

            // Note: EF maps VARBINARY to byte[] (nullable sometimes)
            var salt = user.PasswordSalt ?? throw new InvalidOperationException("User credential data is corrupted (salt missing).");
            var hash = user.PasswordHash ?? throw new InvalidOperationException("User credential data is corrupted (hash missing).");

            if (!PasswordHasher.Verify(password, salt, hash))
            {
                // Wrong password: increment attempts via a tracked context.
                await using var wdb = _factory.CreateDbContext();
                var tracked = await wdb.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId, ct);
                if (tracked != null)
                {
                    tracked.FailedAttempts += 1;
                    if (tracked.FailedAttempts >= 5)
                    {
                        tracked.LockedUntil = DateTime.Now.AddMinutes(15);
                        await wdb.SaveChangesAsync(ct);
                        throw new InvalidOperationException($"تم قفل الحساب بسبب محاولات خاطئة متكررة حتى {tracked.LockedUntil.Value:HH:mm dd/MM/yyyy}. حاول لاحقاً.");
                    }
                    await wdb.SaveChangesAsync(ct);
                }
                throw new InvalidOperationException("Invalid username or password.");
            }

            // Success: reset attempts and record login time.
            await using var sdb = _factory.CreateDbContext();
            var okUser = await sdb.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId, ct);
            if (okUser != null)
            {
                okUser.FailedAttempts = 0;
                okUser.LockedUntil = null;
                okUser.LastLoginAt = DateTime.Now;
                await sdb.SaveChangesAsync(ct);
            }

            return new SessionUser
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = user.DisplayName,
                Roles = user.Roles
            };
        }
    }
}
