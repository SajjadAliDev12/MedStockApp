using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MedStock.Data.Context;
using MedStock.Data.Entities;
using MedStock.Services.DTOs;
using MedStock.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MedStock.Services.Implementations
{
    public sealed class UsersManagementService : IUsersManagementService
    {
        private readonly DbExecutor _db;
        private readonly Data.Context.IDbContextFactory<HospitalInventoryDbContext> _factory;

        public UsersManagementService(DbExecutor db, Data.Context.IDbContextFactory<HospitalInventoryDbContext> factory)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public async Task<IReadOnlyList<UserListRow>> GetListAsync(string? search, CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            var q = db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.Username.Contains(s) || x.DisplayName.Contains(s));
            }

            var list = await q
                .OrderByDescending(x => x.IsActive)
                .ThenBy(x => x.Username)
                .Select(x => new
                {
                    x.UserId,
                    x.Username,
                    x.DisplayName,
                    x.IsActive,
                    Roles = x.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                })
                .ToListAsync(ct);

            return list.Select(x => new UserListRow
            {
                UserId = x.UserId,
                Username = x.Username,
                DisplayName = x.DisplayName,
                IsActive = x.IsActive,
                Roles = string.Join(", ", x.Roles)
            }).ToList();
        }

        public async Task<IReadOnlyList<IdNameRow>> GetRolesAsync(CancellationToken ct = default)
        {
            await using var db = _factory.CreateDbContext();
            return await db.Roles.AsNoTracking()
                .Select(r => new IdNameRow { Id = r.RoleId, Name = r.RoleName, IsActive = true })
                .ToListAsync(ct);
        }

        public Task<int> SaveAsync(UserUpsertRequest request, int modifiedByUserId, CancellationToken ct = default)
        {
            Guard.NotNullOrWhiteSpace(request.Username, "اسم المستخدم");
            Guard.NotNullOrWhiteSpace(request.DisplayName, "الاسم الظاهر");

            return _db.ExecuteAsync(async db =>
            {
                // Check duplicate username
                var exists = await db.Users.AnyAsync(u => u.Username == request.Username && u.UserId != request.UserId, ct);
                if (exists) throw new InvalidOperationException($"اسم المستخدم '{request.Username}' مستخدم بالفعل.");

                User user;

                if (request.UserId.HasValue)
                {
                    // Update
                    user = await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.UserId == request.UserId, ct)
                           ?? throw new InvalidOperationException("المستخدم غير موجود.");

                    user.Username = request.Username;
                    user.DisplayName = request.DisplayName;
                    user.IsActive = request.IsActive;

                    // Update Roles
                    db.UserRoles.RemoveRange(user.UserRoles); // Remove old
                    foreach (var rid in request.RoleIds)
                    {
                        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid, CreatedAt = DateTime.Now });
                    }
                }
                else
                {
                    // Insert
                    if (string.IsNullOrWhiteSpace(request.Password))
                        throw new ArgumentException("كلمة المرور مطلوبة للمستخدم الجديد.");

                    var salt = RandomNumberGenerator.GetBytes(32);
                    var hash = PasswordHasher.ComputeHash(request.Password, salt);

                    user = new User
                    {
                        Username = request.Username,
                        DisplayName = request.DisplayName,
                        IsActive = request.IsActive,
                        PasswordSalt = salt,
                        PasswordHash = hash,
                        CreatedAt = DateTime.Now
                    };
                    db.Users.Add(user);
                    await db.SaveChangesAsync(ct); // Save to get ID

                    foreach (var rid in request.RoleIds)
                    {
                        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = rid, CreatedAt = DateTime.Now });
                    }
                }

                return user.UserId;
            }, ct);
        }

        public Task ToggleActiveAsync(int userId, int modifiedByUserId, CancellationToken ct = default)
        {
            return _db.ExecuteAsync(async db =>
            {
                var user = await db.Users.FindAsync(new object[] { userId }, ct);
                if (user != null) user.IsActive = !user.IsActive;
            }, ct);
        }

        public Task ResetPasswordAsync(int userId, string newPassword, int modifiedByUserId, CancellationToken ct = default)
        {
            Guard.NotNullOrWhiteSpace(newPassword, "كلمة المرور الجديدة");

            return _db.ExecuteAsync(async db =>
            {
                var user = await db.Users.FindAsync(new object[] { userId }, ct)
                       ?? throw new InvalidOperationException("المستخدم غير موجود.");

                var salt = RandomNumberGenerator.GetBytes(32);
                var hash = PasswordHasher.ComputeHash(newPassword, salt);

                user.PasswordSalt = salt;
                user.PasswordHash = hash;
            }, ct);
        }
    }
}