using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Controllers;
using Theatre.Api.Data;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class AdminCommunicationTests
{
    [Fact]
    public async Task DeleteMessage_PermanentlyDeletesMessageRegardlessOfStatus()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.ContactMessages.Add(new ContactMessage
        {
            Id = 1,
            Name = "Visitor",
            Email = "visitor@example.com",
            Subject = "Question",
            Message = "Is the performance accessible?",
            LanguageCode = "en",
            Status = ContactMessageStatus.Read,
            CreatedAt = FakeClock.Now,
            UpdatedAt = FakeClock.Now
        });
        await db.SaveChangesAsync();
        var controller = new AdminCommunicationController(db, new FakeClock());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.DeleteMessage(1, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Empty(await db.ContactMessages.ToListAsync());
        Assert.Contains(await db.AdminActivities.ToListAsync(), x =>
            x.Action == "Deleted" && x.EntityType == "ContactMessage" && x.EntityId == "1");
    }

    private sealed class FakeClock : IClock
    {
        public static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now;
    }
}
