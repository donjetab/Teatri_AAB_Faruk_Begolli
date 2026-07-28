using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record ContactMessageRequest(
    [property: Required, MaxLength(160)]
    string Name,

    [property: Required, EmailAddress, MaxLength(180)]
    string Email,

    [property: Required, MaxLength(220)]
    string Subject,

    [property: Required, MaxLength(5000)]
    string Message,

    [property: Required, RegularExpression("^(sq|en)$")]
    string LanguageCode);

public sealed record ContactMessageResponse(string Message);
