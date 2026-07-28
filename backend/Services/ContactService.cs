using Theatre.Api.Data;
using Theatre.Api.DTOs;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public interface IContactService
{
    Task<ContactMessageResponse> SendAsync(ContactMessageRequest request, CancellationToken cancellationToken);
}

public sealed class ContactService(AppDbContext db, IClock clock) : IContactService
{
    public async Task<ContactMessageResponse> SendAsync(
        ContactMessageRequest request,
        CancellationToken cancellationToken)
    {
        db.ContactMessages.Add(new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Subject = request.Subject.Trim(),
            Message = request.Message.Trim(),
            LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(),
            IsRead = false,
            CreatedAt = clock.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new ContactMessageResponse("Message sent successfully.");
    }
}
