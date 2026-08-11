using Microsoft.AspNetCore.RateLimiting;
using Theatre.Api.Controllers;

namespace Theatre.Api.Tests;

public sealed class ContactSecurityTests
{
    [Fact]
    public void Send_RequiresContactFormRateLimitPolicy()
    {
        var method = typeof(ContactController).GetMethod(nameof(ContactController.Send));
        var attribute = Assert.Single(method!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
            .Cast<EnableRateLimitingAttribute>());

        Assert.Equal("ContactForm", attribute.PolicyName);
    }
}
