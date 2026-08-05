namespace Theatre.Api.DTOs;

public sealed record NavigationItemDto(string RouteKey, int SortOrder, bool ShowInHeader, bool ShowInFooter);
public sealed record NavigationLanguageDto(
    string LanguageCode,
    Dictionary<string, string> Labels,
    string ReserveLabel,
    string FooterLinksTitle,
    string FooterVisitTitle,
    string FooterFollowTitle,
    string FooterNewsletterTitle,
    string FooterNewsletterText,
    string FooterLocationLabel);
public sealed record NavigationConfigurationDto(
    IReadOnlyList<NavigationItemDto> Items,
    IReadOnlyList<NavigationLanguageDto> Translations);
