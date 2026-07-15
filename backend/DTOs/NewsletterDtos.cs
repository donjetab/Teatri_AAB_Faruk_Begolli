using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record NewsletterSubscribeRequest(
    [property: Required]
    [property: EmailAddress]
    [property: MaxLength(180)]
    string Email,

    [property: Required]
    [property: RegularExpression("^(sq|en)$")]
    string PreferredLanguageCode);

public sealed record NewsletterSubscribeResponse(string Message);
