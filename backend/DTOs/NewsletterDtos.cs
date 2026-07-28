using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record NewsletterSubscribeRequest(
    [Required]
    [EmailAddress]
    [MaxLength(180)]
    string Email,

    [Required]
    [RegularExpression("^(sq|en)$")]
    string PreferredLanguageCode);

public sealed record NewsletterSubscribeResponse(string Message);
