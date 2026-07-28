using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(AppDbContext db, IPasswordHasher<AdminUser> passwordHasher, IClock clock) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AdminSessionDto>> Login(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.AdminUsers.SingleOrDefaultAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new ProblemDetails { Title = "Login failed", Detail = "Invalid email or password.", Status = 401 });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = request.RememberMe, ExpiresUtc = clock.UtcNow.AddHours(request.RememberMe ? 168 : 8) });
        user.LastLoginAt = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToSession(user));
    }

    [Authorize(Policy = "Admin")]
    [HttpGet("session")]
    public async Task<ActionResult<AdminSessionDto>> Session(CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        return user is null ? Unauthorized() : Ok(ToSession(user));
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await CurrentUserAsync(cancellationToken);
        if (user is null) return Unauthorized();
        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new ProblemDetails { Title = "Password change failed", Detail = "The current password is incorrect.", Status = 400 });
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private async Task<AdminUser?> CurrentUserAsync(CancellationToken cancellationToken) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? await db.AdminUsers.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken) : null;
    private static AdminSessionDto ToSession(AdminUser user) => new(user.Id, user.Email, user.DisplayName, user.Role.ToString());
}
