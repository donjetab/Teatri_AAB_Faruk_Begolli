using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record ContactMessageRequest(
    [Required, MaxLength(160)]
    string Name,

    [Required, EmailAddress, MaxLength(180)]
    string Email,

    [Required, MaxLength(220)]
    string Subject,

    [Required, MaxLength(5000)]
    string Message,

    [Required, RegularExpression("^(sq|en)$")]
    string LanguageCode);

public sealed record ContactMessageResponse(string Message);
