using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminUserListDto(int Id, string DisplayName, string Email, string Role, bool IsActive,
    DateTimeOffset? LastLoginAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record SaveAdminUserRequest([Required, MaxLength(160)] string DisplayName,
    [Required, EmailAddress, MaxLength(180)] string Email, [Required] string Role, bool IsActive,
    [MinLength(12)] string? Password);
public sealed record ResetAdminPasswordRequest([Required, MinLength(12)] string NewPassword);
public sealed record AdminActivityDto(long Id, string AdminName, string Action, string EntityType,
    string? EntityId, string? Summary, DateTimeOffset CreatedAt);
public sealed record SystemStatusDto(string DatabaseStatus, long MediaStorageBytes, int MediaFileCount,
    DateTimeOffset? LastDatabaseBackupAt, DateTimeOffset? LastMediaBackupAt, string BackupManagement,
    string Environment, string ApplicationVersion, int BrokenMediaReferences, int FailedUploads,
    IReadOnlyList<BrokenMediaDto> BrokenMedia, IReadOnlyList<OperationalEventDto> RecentErrors);
public sealed record OperationalEventDto(long Id, string EventType, string Severity, string Summary,
    string? RequestPath, string? CorrelationId, DateTimeOffset CreatedAt);
public sealed record AdminSettingsDto(string DefaultLanguage, IReadOnlyList<string> SupportedLanguages,
    int DefaultPageSize, long MaximumUploadBytes, IReadOnlyList<string> AllowedUploadTypes,
    string DateFormat, string TimeFormat, bool MissingTranslationsBlockPublication);
public sealed record SaveAdminSettingsRequest([Required, RegularExpression("^(sq|en)$")] string DefaultLanguage);
