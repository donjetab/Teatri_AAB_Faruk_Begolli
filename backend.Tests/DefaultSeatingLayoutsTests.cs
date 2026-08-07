using Theatre.Api.Services;

namespace Theatre.Api.Tests;

public sealed class DefaultSeatingLayoutsTests
{
    [Fact]
    public void FarukBegolli_HasReferenceRowsAndSeatCounts()
    {
        var template = DefaultSeatingLayouts.Create(DefaultSeatingLayouts.FarukKey, 1, DateTimeOffset.UtcNow);
        var rows = template.Sections.SelectMany(x => x.Rows).OrderBy(x => x.DisplayOrder).ToList();
        Assert.Equal(12, rows.Count);
        Assert.Equal(new[] { 23, 24, 25, 26, 27, 26, 25, 26, 27, 28, 14, 14 }, rows.Select(x => x.Seats.Count));
        Assert.Equal(285, rows.Sum(x => x.Seats.Count));
        Assert.Contains(rows.Take(10).SelectMany(x => x.Seats), x => x.Rotation != 0);
        var upperSeats = rows[10].Seats.ToList();
        Assert.True(upperSeats[9].PositionX - upperSeats[8].PositionX > 40);
    }

    [Fact]
    public void Kamertal_HasStageAndThreeReferenceSections()
    {
        var template = DefaultSeatingLayouts.Create(DefaultSeatingLayouts.KamertalKey, 1, DateTimeOffset.UtcNow);
        Assert.Equal(new[] { "B", "A", "C" }, template.Sections.Select(x => x.Name));
        Assert.Equal(new[] { 43, 30, 30 }, template.Sections.Select(x => x.Rows.Sum(r => r.Seats.Count)));
        Assert.Equal(103, template.Sections.Sum(x => x.Rows.Sum(r => r.Seats.Count)));
        Assert.Equal("STAGE", template.StageLabel);
        var sections = template.Sections.ToList();
        Assert.All(sections[1].Rows.SelectMany(x => x.Seats), x => Assert.Equal(-90, x.Rotation));
        Assert.All(sections[2].Rows.SelectMany(x => x.Seats), x => Assert.Equal(90, x.Rotation));
    }
}
