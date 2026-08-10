using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Reflection;
using Theatre.Api.Data;
using Theatre.Api.Data.Seed;
using Theatre.Api.Middleware;
using Theatre.Api.Services;
using Theatre.Api.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddScoped<IHomepageService, HomepageService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IPhoneNumberNormalizer, PhoneNumberNormalizer>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IAdminDeletionService, AdminDeletionService>();
builder.Services.AddHostedService<SeatingTemplateInitializer>();
builder.Services.AddSingleton<IClock, Theatre.Api.Services.SystemClock>();
builder.Services.AddHostedService<PerformanceStatusUpdater>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddOptions<UploadOptions>()
    .Bind(builder.Configuration.GetSection(UploadOptions.SectionName))
    .Validate(x => x.MaxBytes is > 0 and <= 52_428_800, "Uploads:MaxBytes must be between 1 byte and 50 MB.")
    .Validate(x => x.AllowedMimeTypes.Length > 0, "At least one upload MIME type must be configured.")
    .ValidateOnStart();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AdminLogin", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TeatriAab.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
        options.Events.OnValidatePrincipal = async context =>
        {
            if (!int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var user = await db.AdminUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, context.HttpContext.RequestAborted);
            var role = context.Principal?.FindFirstValue(ClaimTypes.Role);
            if (user is null || !user.IsActive || !string.Equals(role, user.Role.ToString(), StringComparison.Ordinal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync();
            }
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)))
    .AddPolicy("SuperAdmin", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin)))
    .AddPolicy("ViewReservations", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)))
    .AddPolicy("ManageReservations", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)))
    .AddPolicy("BlockSeats", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)))
    .AddPolicy("ExportCustomerData", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin)))
    .AddPolicy("ManageTheatreSchemas", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin)))
    .AddPolicy("ViewReservationAudit", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDevelopment", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var origins = app.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    if (origins.Length == 0 || origins.Any(x => x.Contains("localhost", StringComparison.OrdinalIgnoreCase)))
        throw new InvalidOperationException("Production Cors:AllowedOrigins must contain the deployed HTTPS admin origin and must not use localhost.");
    var publicUrl = app.Configuration["PublicSite:BaseUrl"];
    if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri) || publicUri.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException("Production PublicSite:BaseUrl must be an absolute HTTPS URL.");
    var allowedHosts = app.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        throw new InvalidOperationException("Production AllowedHosts must list the deployed host names and must not use '*'.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("FrontendDevelopment");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AdminRequestOriginMiddleware>();
app.UseAuthorization();
app.UseMiddleware<AdminActivityMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await LegacyGalleryImporter.ImportAsync(db, app.Environment);
    await LegacyShowGalleryImporter.ImportAsync(db, app.Environment);
    if (app.Configuration.GetValue("Seed:EnableDevelopmentSeed", false))
        await DevelopmentDataSeeder.SeedAsync(db, app.Environment);
    await ContentSlugNormalizer.NormalizeAsync(db);

    var bootstrapEmail = app.Configuration["AdminBootstrap:Email"]?.Trim().ToLowerInvariant();
    var bootstrapPassword = app.Configuration["AdminBootstrap:Password"];
    if (!string.IsNullOrWhiteSpace(bootstrapEmail) && !string.IsNullOrWhiteSpace(bootstrapPassword)
        && !await db.AdminUsers.AnyAsync())
    {
        if (bootstrapPassword.Length < 12)
            throw new InvalidOperationException("AdminBootstrap:Password must contain at least 12 characters.");
        var admin = new AdminUser
        {
            Email = bootstrapEmail,
            DisplayName = app.Configuration["AdminBootstrap:DisplayName"] ?? "Super Admin",
            Role = AdminRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        admin.PasswordHash = new PasswordHasher<AdminUser>().HashPassword(admin, bootstrapPassword);
        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();
    }
}

app.Run();
