using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Theatre.Api.Controllers;
using Theatre.Api.Data;
using Theatre.Api.Models;
using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class ReservationCompletionTests
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PerformanceRules_EnforceOpeningClosingPausedAndSeatLimit()
    {
        var performance = new ShowPerformance { ReservationsEnabled = false };
        Assert.False(ReservationRules.Evaluate(performance, Now).IsAvailable);
        performance.ReservationsEnabled = true; performance.ReservationOpensAtUtc = Now.AddMinutes(1);
        Assert.False(ReservationRules.Evaluate(performance, Now).IsAvailable);
        performance.ReservationOpensAtUtc = Now.AddMinutes(-1); performance.ReservationClosesAtUtc = Now.AddMinutes(-1);
        Assert.False(ReservationRules.Evaluate(performance, Now).IsAvailable);
        performance.ReservationClosesAtUtc = Now.AddMinutes(1); performance.MaxSeatsPerReservation = 2;
        Assert.False(ReservationRules.Evaluate(performance, Now, 3).IsAvailable);
        Assert.True(ReservationRules.Evaluate(performance, Now, 2).IsAvailable);
    }

    [Fact]
    public async Task ExistingNormalizedPhone_ReusesCustomer_AndCreatesMultiSeatReservation()
    {
        await using var db = CreateDb(); var setup = SeedPerformance(db); await db.SaveChangesAsync();
        var service = new ReservationService(db, new PhoneNumberNormalizer(), new FakeClock());
        var first = await service.FindOrCreateCustomerAsync("First", "044 123 456", "+383", null, default); await db.SaveChangesAsync();
        var second = await service.FindOrCreateCustomerAsync("Changed", "+38344123456", null, null, default);
        Assert.Same(first, second);
        var reservation = await service.CreateAsync(setup.Performance.Id, first, setup.Seats.Select(x => x.Id).ToArray(), ReservationSource.Admin, null, default);
        Assert.Equal(2, reservation.SeatAllocations.Count); Assert.Equal(ReservationSource.Admin, reservation.Source);
    }

    [Fact]
    public async Task ReleasingAndReactivating_PreservesAllocationHistory()
    {
        await using var db = CreateDb(); var setup = SeedPerformance(db); await db.SaveChangesAsync();
        var service = new ReservationService(db, new PhoneNumberNormalizer(), new FakeClock()); var customer = await service.FindOrCreateCustomerAsync("Customer", "44123456", "+383", null, default);
        var reservation = await service.CreateAsync(setup.Performance.Id, customer, [setup.Seats[0].Id], ReservationSource.Admin, null, default);
        await service.SetStatusAsync(reservation, ReservationStatus.Cancelled, 1, default); Assert.False(await db.SeatAllocations.AnyAsync(x => x.ReservationId == reservation.Id && x.IsActive));
        await service.SetStatusAsync(reservation, ReservationStatus.Active, 1, default); Assert.True(await db.SeatAllocations.AnyAsync(x => x.ReservationId == reservation.Id && x.IsActive)); Assert.True(await db.SeatAllocations.CountAsync(x => x.ReservationId == reservation.Id) > 1);
    }

    [Fact]
    public void ActiveSeatAllocation_HasDatabaseUniqueGuardForConcurrentBookings()
    {
        using var db = CreateDb(); var index = db.Model.FindEntityType(typeof(SeatAllocation))!.GetIndexes().Single(x => x.Properties.Select(p => p.Name).SequenceEqual([nameof(SeatAllocation.PerformanceSeatId)]));
        Assert.True(index.IsUnique); Assert.Equal("[IsActive] = 1", index.GetFilter());
    }

    [Theory]
    [InlineData(nameof(AdminReservationsController.Export), "ExportCustomerData")]
    [InlineData(nameof(AdminReservationsController.Block), "BlockSeats")]
    [InlineData(nameof(AdminReservationsController.SaveLayout), "ManageTheatreSchemas")]
    [InlineData(nameof(AdminReservationsController.ReservationAudit), "ViewReservationAudit")]
    public void RestrictedActions_RequireNamedBackendPolicy(string method, string policy)
    {
        var attribute = typeof(AdminReservationsController).GetMethods().Single(x => x.Name == method).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(policy, attribute.Policy);
    }

    private static (ShowPerformance Performance, PerformanceSeat[] Seats) SeedPerformance(AppDbContext db)
    {
        var show = new Show { Id = 1, CreatedAt = Now, UpdatedAt = Now };
        var performance = new ShowPerformance { Id = 1, Show = show, StartDateTimeUtc = Now.AddDays(1), ReservationsEnabled = true, ReservationMode = ReservationMode.Internal, CreatedAt = Now, UpdatedAt = Now };
        var layout = new PerformanceSeatingLayout { Id = 1, Performance = performance, CreatedAt = Now, UpdatedAt = Now };
        var seats = new[] { new PerformanceSeat { Id = 1, Layout = layout, SectionName = "A", RowLabel = "1", SeatLabel = "1", IsActive = true }, new PerformanceSeat { Id = 2, Layout = layout, SectionName = "A", RowLabel = "1", SeatLabel = "2", IsActive = true } };
        db.Shows.Add(show); db.ShowPerformances.Add(performance); db.PerformanceSeatingLayouts.Add(layout); db.PerformanceSeats.AddRange(seats); return (performance, seats);
    }
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private sealed class FakeClock : IClock { public DateTimeOffset UtcNow => Now; }
}
