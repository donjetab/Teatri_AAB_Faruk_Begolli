namespace Theatre.Api.Models;

public enum ShowStatus
{
    Draft,
    Published,
    Archived,
    Cancelled
}

public enum PerformanceStatus
{
    Scheduled,
    SoldOut,
    Postponed,
    Cancelled,
    Completed
}

public enum GalleryAlbumType
{
    General,
    Show,
    NewsArticle,
    PitfEdition
}
