using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class PhoneNumberNormalizerTests
{
    private readonly PhoneNumberNormalizer normalizer = new();

    [Theory]
    [InlineData("044 123 456", "+383", "+38344123456")]
    [InlineData("+1 (202) 555-0123", null, "+12025550123")]
    [InlineData("0044 20 7946 0958", null, "+442079460958")]
    public void Normalize_ReturnsInternationalFormat(string input, string? prefix, string expected) => Assert.Equal(expected, normalizer.Normalize(input, prefix));

    [Theory]
    [InlineData("123")]
    [InlineData("++38344123456")]
    public void Normalize_RejectsInvalidNumbers(string input) => Assert.Throws<ValidationException>(() => normalizer.Normalize(input));
}
