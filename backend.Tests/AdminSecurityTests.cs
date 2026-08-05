using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Theatre.Api.Controllers;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class AdminSecurityTests
{
    [Fact]
    public async Task Login_DoesNotAllowInactiveAdministrator()
    {
        await using var db = CreateDb();
        var hasher = new PasswordHasher<AdminUser>();
        var user = User(1, AdminRole.ContentEditor, false);
        user.PasswordHash = hasher.HashPassword(user, "A-valid-password-123!");
        db.AdminUsers.Add(user);
        await db.SaveChangesAsync();
        var controller = new AdminAuthController(db, hasher, new FakeClock());

        var result = await controller.Login(new AdminLoginRequest(user.Email, "A-valid-password-123!", false), CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateUser_ProtectsFinalActiveSuperAdmin()
    {
        await using var db = CreateDb();
        db.AdminUsers.Add(User(1, AdminRole.SuperAdmin, true));
        db.AdminUsers.Add(User(2, AdminRole.ContentEditor, true));
        await db.SaveChangesAsync();
        var controller = SystemController(db, currentUserId: 2);

        var result = await controller.UpdateUser(1,
            new SaveAdminUserRequest("Owner", "owner@example.com", "ContentEditor", false, null), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.True((await db.AdminUsers.FindAsync(1))!.IsActive);
    }

    [Fact]
    public async Task UpdateUser_PreventsAdministratorRemovingOwnSuperAdminAccess()
    {
        await using var db = CreateDb();
        db.AdminUsers.Add(User(1, AdminRole.SuperAdmin, true));
        db.AdminUsers.Add(User(2, AdminRole.SuperAdmin, true));
        await db.SaveChangesAsync();
        var controller = SystemController(db, currentUserId: 1);

        var result = await controller.UpdateUser(1,
            new SaveAdminUserRequest("Owner", "owner@example.com", "ContentEditor", true, null), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(AdminRole.SuperAdmin, (await db.AdminUsers.FindAsync(1))!.Role);
    }

    private static AdminSystemController SystemController(AppDbContext db, int currentUserId)
    {
        var controller = new AdminSystemController(db, new PasswordHasher<AdminUser>(), new FakeClock(),
            new FakeEnvironment(), Options.Create(new UploadOptions()), new ConfigurationBuilder().Build());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                    new Claim(ClaimTypes.Name, "Current administrator"),
                    new Claim(ClaimTypes.Role, nameof(AdminRole.SuperAdmin))
                ], "Test"))
            }
        };
        return controller;
    }

    private static AdminUser User(int id, AdminRole role, bool active) => new()
    {
        Id = id, DisplayName = id == 1 ? "Owner" : "Editor", Email = id == 1 ? "owner@example.com" : "editor@example.com",
        PasswordHash = "not-used", Role = role, IsActive = active,
        CreatedAt = FakeClock.Now, UpdatedAt = FakeClock.Now
    };

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeClock : IClock
    {
        public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
