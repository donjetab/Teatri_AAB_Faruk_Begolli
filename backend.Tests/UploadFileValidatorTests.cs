using Microsoft.AspNetCore.Http;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class UploadFileValidatorTests
{
    private static readonly UploadOptions Options = new()
    {
        MaxBytes = 1024,
        AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp", "video/mp4", "application/pdf"]
    };

    [Theory]
    [InlineData("photo.jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF, 0x00 })]
    [InlineData("photo.png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })]
    [InlineData("document.pdf", "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })]
    public async Task ValidateAsync_AcceptsMatchingSignature(string name, string mime, byte[] content)
    {
        var result = await UploadFileValidator.ValidateAsync(File(name, mime, content), Options, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(mime, result.MimeType);
    }

    [Fact]
    public async Task ValidateAsync_RejectsSpoofedImage()
    {
        var result = await UploadFileValidator.ValidateAsync(
            File("malware.jpg", "image/jpeg", "<script>alert(1)</script>"u8.ToArray()), Options, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_RejectsSvgEvenWhenBrowserMimeTypeIsDeclared()
    {
        var result = await UploadFileValidator.ValidateAsync(
            File("active.svg", "image/svg+xml", "<svg onload='alert(1)'/>"u8.ToArray()), Options, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_RejectsFileLargerThanConfiguredLimit()
    {
        var options = new UploadOptions { MaxBytes = 3, AllowedMimeTypes = ["image/jpeg"] };
        var result = await UploadFileValidator.ValidateAsync(
            File("photo.jpg", "image/jpeg", [0xFF, 0xD8, 0xFF, 0x00]), options, CancellationToken.None);
        Assert.Null(result);
    }

    private static FormFile File(string name, string mime, byte[] content)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", name) { Headers = new HeaderDictionary(), ContentType = mime };
    }
}
