namespace Theatre.Api.Services;

public sealed class UploadOptions
{
    public const string SectionName = "Uploads";
    public long MaxBytes { get; set; } = 20 * 1024 * 1024;
    public string[] AllowedMimeTypes { get; set; } =
        ["image/jpeg", "image/png", "image/webp", "video/mp4", "application/pdf"];
}
