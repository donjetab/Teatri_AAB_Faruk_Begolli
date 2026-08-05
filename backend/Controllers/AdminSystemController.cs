using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;
using Microsoft.Extensions.Options;

namespace Theatre.Api.Controllers;

[ApiController, Authorize(Policy = "SuperAdmin"), Route("api/admin/system")]
public sealed class AdminSystemController(AppDbContext db, IPasswordHasher<AdminUser> hasher, IClock clock,
    IWebHostEnvironment environment, IOptions<UploadOptions> uploadOptions, IConfiguration configuration) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserListDto>>> Users(CancellationToken token) => Ok(await db.AdminUsers
        .AsNoTracking().OrderBy(x => x.DisplayName).Select(x => ToUser(x)).ToListAsync(token));

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserListDto>> CreateUser(SaveAdminUserRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Password)) return ValidationProblem("A password of at least 12 characters is required.");
        if (!Enum.TryParse<AdminRole>(request.Role, true, out var role)) return ValidationProblem("Choose a valid role.");
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.AdminUsers.AnyAsync(x => x.Email == email, token)) return Conflict(new ProblemDetails { Title = "Email already used", Detail = "An administrator already uses this email address." });
        var user = new AdminUser { DisplayName = request.DisplayName.Trim(), Email = email, Role = role, IsActive = request.IsActive, CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.AdminUsers.Add(user); Activity("Created", "AdminUser", null, $"Created administrator {email}"); await db.SaveChangesAsync(token);
        return Created("", ToUser(user));
    }

    [HttpPut("users/{id:int}")]
    public async Task<ActionResult<AdminUserListDto>> UpdateUser(int id, SaveAdminUserRequest request, CancellationToken token)
    {
        var user = await db.AdminUsers.FindAsync([id], token); if (user is null) return NotFound();
        if (!Enum.TryParse<AdminRole>(request.Role, true, out var role)) return ValidationProblem("Choose a valid role.");
        var currentId = CurrentId();
        if (id == currentId && (!request.IsActive || role != AdminRole.SuperAdmin)) return Conflict(new ProblemDetails { Title = "Your access is protected", Detail = "You cannot deactivate your own account or remove your own Super Admin role." });
        if (user.Role == AdminRole.SuperAdmin && user.IsActive && (!request.IsActive || role != AdminRole.SuperAdmin) && await ActiveSuperAdmins(token) <= 1)
            return Conflict(new ProblemDetails { Title = "Final Super Admin", Detail = "The final active Super Admin cannot be deactivated or reassigned." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.AdminUsers.AnyAsync(x => x.Email == email && x.Id != id, token)) return Conflict(new ProblemDetails { Title = "Email already used", Detail = "An administrator already uses this email address." });
        user.DisplayName = request.DisplayName.Trim(); user.Email = email; user.Role = role; user.IsActive = request.IsActive; user.UpdatedAt = clock.UtcNow;
        Activity("Updated", "AdminUser", id.ToString(), $"Updated administrator {email}"); await db.SaveChangesAsync(token); return Ok(ToUser(user));
    }

    [HttpPost("users/{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, ResetAdminPasswordRequest request, CancellationToken token)
    { var user = await db.AdminUsers.FindAsync([id], token); if (user is null) return NotFound(); user.PasswordHash = hasher.HashPassword(user, request.NewPassword); user.UpdatedAt = clock.UtcNow; Activity("Reset password", "AdminUser", id.ToString(), $"Reset password for {user.Email}"); await db.SaveChangesAsync(token); return NoContent(); }

    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken token)
    { var user = await db.AdminUsers.FindAsync([id], token); if (user is null) return NotFound(); if (id == CurrentId()) return Conflict(new ProblemDetails { Title = "Your access is protected", Detail = "You cannot remove your own account." }); if (user.Role == AdminRole.SuperAdmin && user.IsActive && await ActiveSuperAdmins(token) <= 1) return Conflict(new ProblemDetails { Title = "Final Super Admin", Detail = "The final active Super Admin cannot be removed." }); user.IsActive = false; user.UpdatedAt = clock.UtcNow; Activity("Removed access", "AdminUser", id.ToString(), $"Removed access for {user.Email}"); await db.SaveChangesAsync(token); return NoContent(); }

    [HttpDelete("users/{id:int}/permanent")]
    public async Task<IActionResult> PermanentlyDeleteUser(int id, CancellationToken token)
    {
        var user = await db.AdminUsers.FindAsync([id], token); if (user is null) return NotFound();
        if (id == CurrentId()) return Conflict(new ProblemDetails { Title = "Your access is protected", Detail = "You cannot delete your own account." });
        if (user.IsActive) return Conflict(new ProblemDetails { Title = "Remove access first", Detail = "Deactivate this administrator before permanently deleting the account." });
        await db.AdminActivities.Where(x => x.AdminUserId == id && x.AdminDisplayName == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.AdminDisplayName, user.DisplayName), token);
        db.AdminUsers.Remove(user); Activity("Deleted account", "AdminUser", id.ToString(), $"Permanently deleted the disabled administrator account for {user.DisplayName}");
        await db.SaveChangesAsync(token); return NoContent();
    }

    [HttpGet("activity-users")]
    public async Task<ActionResult<IReadOnlyList<string>>> ActivityUsers(CancellationToken token)
    {
        var currentNames = await db.AdminUsers.AsNoTracking().Select(x => x.DisplayName).ToListAsync(token);
        var historicalNames = await db.AdminActivities.AsNoTracking().Where(x => x.AdminDisplayName != null)
            .Select(x => x.AdminDisplayName!).Distinct().ToListAsync(token);
        return Ok(currentNames.Concat(historicalNames).Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
    }

    [HttpGet("activity-filters")]
    public async Task<IActionResult> ActivityFilters(CancellationToken token)
    {
        var actions = await db.AdminActivities.AsNoTracking().Select(x => x.Action).Distinct().OrderBy(x => x).ToListAsync(token);
        var entityTypes = await db.AdminActivities.AsNoTracking().Select(x => x.EntityType).Distinct().OrderBy(x => x).ToListAsync(token);
        return Ok(new { actions, entityTypes });
    }

    [HttpGet("activity")]
    public async Task<ActionResult<PagedResultDto<AdminActivityDto>>> ActivityLog([FromQuery] string? search, [FromQuery] string? action, [FromQuery] string? entityType, [FromQuery] string? adminName, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken token = default)
    { page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); var query = db.AdminActivities.AsNoTracking().AsQueryable(); if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => (x.Summary ?? "").Contains(search) || x.Action.Contains(search)); if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action); if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType); if (!string.IsNullOrWhiteSpace(adminName)) query = query.Where(x => (x.AdminUser != null && x.AdminUser.DisplayName == adminName) || x.AdminDisplayName == adminName); if (from.HasValue) query = query.Where(x => x.CreatedAt >= from); if (to.HasValue) query = query.Where(x => x.CreatedAt < to); var total = await query.CountAsync(token); var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new AdminActivityDto(x.Id, x.AdminUser != null ? x.AdminUser.DisplayName : x.AdminDisplayName ?? "System", x.Action, x.EntityType, x.EntityId, x.Summary, x.CreatedAt)).ToListAsync(token); return Ok(new PagedResultDto<AdminActivityDto>(items, page, pageSize, total)); }

    [HttpGet("status")]
    public async Task<ActionResult<SystemStatusDto>> Status(CancellationToken token)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadRoot = Path.Combine(webRoot, "uploads");
        var files = Directory.Exists(uploadRoot) ? Directory.EnumerateFiles(uploadRoot, "*", SearchOption.AllDirectories).ToList() : [];
        long bytes = files.Sum(x => new FileInfo(x).Length);
        var media = await db.MediaAssets.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.FileName, x.FileUrl }).ToListAsync(token);
        var broken = media.Where(x => !MediaStoragePath.Exists(environment, x.FileUrl)).Select(x => new BrokenMediaDto(x.Id, x.FileName, x.FileUrl)).ToList();
        var connected = await db.Database.CanConnectAsync(token);
        var recent = await db.OperationalEvents.AsNoTracking().Where(x => x.Severity == "Error").OrderByDescending(x => x.CreatedAt).Take(20)
            .Select(x => new OperationalEventDto(x.Id, x.EventType, x.Severity, x.Summary, x.RequestPath, x.CorrelationId, x.CreatedAt)).ToListAsync(token);
        var failedUploads = await db.OperationalEvents.CountAsync(x => x.EventType == "FailedUpload", token);
        DateTimeOffset? databaseBackup = configuration.GetValue<DateTimeOffset?>("BackupStatus:LastDatabaseBackupAt");
        DateTimeOffset? mediaBackup = configuration.GetValue<DateTimeOffset?>("BackupStatus:LastMediaBackupAt");
        var provider = configuration["BackupStatus:Provider"];
        var backupMessage = string.IsNullOrWhiteSpace(provider)
            ? "Backups are managed externally; no provider has reported status to this application."
            : $"Backup status is supplied by {provider}. This page never runs a backup or restore.";
        return Ok(new SystemStatusDto(connected ? "Connected" : "Unavailable", bytes, files.Count, databaseBackup, mediaBackup,
            backupMessage, environment.EnvironmentName, Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            broken.Count, failedUploads, broken, recent));
    }

    [HttpGet("settings")]
    public async Task<ActionResult<AdminSettingsDto>> Settings(CancellationToken token)
    { var languages = await db.Languages.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Id).ToListAsync(token); return Ok(new AdminSettingsDto(languages.First(x => x.IsDefault).Code, languages.Select(x => x.Code).ToList(), 20, uploadOptions.Value.MaxBytes, uploadOptions.Value.AllowedMimeTypes, "Locale based", "Locale based", true)); }

    [HttpPut("settings")]
    public async Task<ActionResult<AdminSettingsDto>> SaveSettings(SaveAdminSettingsRequest request, CancellationToken token)
    { var languages = await db.Languages.Where(x => x.IsActive).ToListAsync(token); if (languages.All(x => x.Code != request.DefaultLanguage)) return ValidationProblem("Choose an active language."); var current = languages.First(x => x.IsDefault); var next = languages.First(x => x.Code == request.DefaultLanguage); if (current.Id != next.Id) { await using var transaction = await db.Database.BeginTransactionAsync(token); current.IsDefault = false; await db.SaveChangesAsync(token); next.IsDefault = true; Activity("Updated", "Settings", null, $"Changed default language to {request.DefaultLanguage}"); await db.SaveChangesAsync(token); await transaction.CommitAsync(token); } return await Settings(token); }

    private int CurrentId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private Task<int> ActiveSuperAdmins(CancellationToken token) => db.AdminUsers.CountAsync(x => x.IsActive && x.Role == AdminRole.SuperAdmin, token);
    private static AdminUserListDto ToUser(AdminUser x) => new(x.Id, x.DisplayName, x.Email, x.Role.ToString(), x.IsActive, x.LastLoginAt, x.CreatedAt, x.UpdatedAt);
    private void Activity(string action, string type, string? id, string summary) => db.AdminActivities.Add(new AdminActivity { AdminUserId = CurrentId(), AdminDisplayName = User.FindFirstValue(ClaimTypes.Name), Action = action, EntityType = type, EntityId = id, Summary = summary, CreatedAt = clock.UtcNow });
}
