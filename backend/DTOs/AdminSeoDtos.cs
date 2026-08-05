namespace Theatre.Api.DTOs;

public sealed record AdminContentIssueDto(string Severity, string Category, string ContentType,
    string Title, string Detail, string AdminPath);

public sealed record AdminSeoOverviewDto(int Errors, int Warnings, int Information,
    IReadOnlyList<AdminContentIssueDto> Issues);

public sealed record BrokenMediaDto(int Id, string FileName, string FileUrl);
