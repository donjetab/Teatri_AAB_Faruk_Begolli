using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record AdminContactMessageDto(int Id, string Name, string Email, string Subject, string Message,
    string LanguageCode, string Status, string? InternalNotes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record AdminContactMessageListDto(IReadOnlyList<AdminContactMessageDto> Items, int Page, int PageSize, int TotalCount, int UnreadCount);
public sealed record UpdateContactMessageRequest([Required] string Status, [MaxLength(4000)] string? InternalNotes);

public sealed record AdminSubscriberDto(int Id, string Email, string PreferredLanguageCode, bool IsActive,
    DateTimeOffset SubscribedAt, DateTimeOffset? UnsubscribedAt, string? Source);
public sealed record AdminSubscriberListDto(IReadOnlyList<AdminSubscriberDto> Items, int Page, int PageSize, int TotalCount);
