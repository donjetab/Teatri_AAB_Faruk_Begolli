using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using System.Reflection;
using Theatre.Api.Data;
using Theatre.Api.Data.Seed;
using Theatre.Api.Middleware;
using Theatre.Api.Services;
using Theatre.Api.Models;

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
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddHostedService<PerformanceStatusUpdater>();
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TeatriAab.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin), nameof(AdminRole.ContentEditor)))
    .AddPolicy("SuperAdmin", policy => policy.RequireRole(nameof(AdminRole.SuperAdmin)));

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
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("FrontendDevelopment");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AdminActivityMiddleware>();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await LegacyGalleryImporter.ImportAsync(db, app.Environment);
    if (app.Configuration.GetValue("Seed:EnableDevelopmentSeed", false))
        await DevelopmentDataSeeder.SeedAsync(db, app.Environment);

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
