using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public interface INewsletterService
{
    Task<NewsletterSubscribeResponse> SubscribeAsync(NewsletterSubscribeRequest request, CancellationToken cancellationToken);
}

public sealed class NewsletterService(AppDbContext db, IClock clock) : INewsletterService
{
    public async Task<NewsletterSubscribeResponse> SubscribeAsync(NewsletterSubscribeRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        if (!IsValidEmail(normalizedEmail))
        {
            throw new ValidationException("Please provide a valid email address.");
        }

        var languageCode = request.PreferredLanguageCode.Trim().ToLowerInvariant();
        if (languageCode is not ("sq" or "en"))
        {
            throw new ValidationException("Preferred language must be 'sq' or 'en'.");
        }

        var alreadyActive = await db.NewsletterSubscribers
            .AnyAsync(x => x.Email == normalizedEmail && x.IsActive, cancellationToken);

        if (alreadyActive)
        {
            throw new ConflictException("This email is already subscribed.");
        }

        var inactiveSubscriber = await db.NewsletterSubscribers
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail && !x.IsActive, cancellationToken);

        if (inactiveSubscriber is null)
        {
            db.NewsletterSubscribers.Add(new NewsletterSubscriber
            {
                Email = normalizedEmail,
                PreferredLanguageCode = languageCode,
                IsActive = true,
                SubscribedAt = clock.UtcNow,
                UnsubscribedAt = null
            });
        }
        else
        {
            inactiveSubscriber.PreferredLanguageCode = languageCode;
            inactiveSubscriber.IsActive = true;
            inactiveSubscriber.SubscribedAt = clock.UtcNow;
            inactiveSubscriber.UnsubscribedAt = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return new NewsletterSubscribeResponse("Subscription completed successfully.");
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

public sealed class ValidationException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);
