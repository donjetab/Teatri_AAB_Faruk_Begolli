namespace Theatre.Api.Services;

public sealed record ValidatedUpload(string MimeType, string Extension);

public static class UploadFileValidator
{
    private static readonly Dictionary<string, string[]> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/webp"] = [".webp"],
        ["video/mp4"] = [".mp4"],
        ["application/pdf"] = [".pdf"]
    };

    public static async Task<ValidatedUpload?> ValidateAsync(IFormFile file, UploadOptions options, CancellationToken token)
    {
        if (file.Length is <= 0 || file.Length > options.MaxBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var mime = file.ContentType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!options.AllowedMimeTypes.Contains(mime, StringComparer.OrdinalIgnoreCase)
            || !Extensions.TryGetValue(mime, out var extensions) || !extensions.Contains(extension)) return null;

        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(), token);
        if (!MatchesSignature(mime, header.AsSpan(0, read))) return null;
        return new ValidatedUpload(mime, extension);
    }

    private static bool MatchesSignature(string mime, ReadOnlySpan<byte> bytes) => mime switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
        "video/mp4" => bytes.Length >= 12 && bytes[4..8].SequenceEqual("ftyp"u8),
        "application/pdf" => bytes.Length >= 5 && bytes[..5].SequenceEqual("%PDF-"u8),
        _ => false
    };
}
