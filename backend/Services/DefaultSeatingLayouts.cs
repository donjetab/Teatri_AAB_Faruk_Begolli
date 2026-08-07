using Microsoft.EntityFrameworkCore;
using Theatre.Api.Data;
using Theatre.Api.Models;

namespace Theatre.Api.Services;

public static class DefaultSeatingLayouts
{
    public const string FarukKey = "faruk-begolli";
    public const string KamertalKey = "kamertal";

    public static SeatingTemplate Create(string key, int locationId, DateTimeOffset now) => key switch
    {
        FarukKey => CreateFaruk(locationId, now),
        KamertalKey => CreateKamertal(locationId, now),
        _ => throw new ValidationException("No saved default exists for this theatre.")
    };

    public static string? Identify(string venue, string templateName)
    {
        var text = $"{venue} {templateName}";
        if (text.Contains("Kamertal", StringComparison.OrdinalIgnoreCase)) return KamertalKey;
        if (text.Contains("Faruk", StringComparison.OrdinalIgnoreCase) || text.Contains("Begolli", StringComparison.OrdinalIgnoreCase)) return FarukKey;
        return null;
    }

    private static SeatingTemplate CreateFaruk(int locationId, DateTimeOffset now)
    {
        var template = Base(locationId, "Teatri AAB “Faruk Begolli”", 1350, 1000, now);
        template.StageLabel = "STAGE"; template.StageX = 230; template.StageY = 920; template.StageWidth = 890; template.StageHeight = 54;
        var main = new SeatingTemplateSection { Name = "Main", DisplayOrder = 1 };
        int[] counts = [23, 24, 25, 26, 27, 26, 25, 26, 27, 28];
        for (var rowIndex = 0; rowIndex < counts.Length; rowIndex++)
        {
            var rowNumber = rowIndex + 1; var count = counts[rowIndex];
            var row = new SeatingTemplateRow { Label = ((char)('A' + rowIndex)).ToString(), DisplayOrder = rowNumber };
            const decimal gap = 44m; var span = (count - 1) * gap; var left = (1350m - span) / 2m; var baseY = 820m - rowIndex * 57m;
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0.5m : (decimal)i / (count - 1); var centered = t * 2m - 1m;
                row.Seats.Add(Seat((i + 1).ToString(), i + 1, left + i * gap, baseY + 48m * centered * centered, centered * 10m));
            }
            main.Rows.Add(row);
        }
        template.Sections.Add(main);
        var upper = new SeatingTemplateSection { Name = "Upper", DisplayOrder = 2 };
        foreach (var rowNumber in new[] { 11, 12 })
        {
            var row = new SeatingTemplateRow { Label = ((char)('A' + rowNumber - 1)).ToString(), DisplayOrder = rowNumber };
            for (var number = 1; number <= 14; number++)
            {
                var x = number <= 9 ? 330m + (number - 1) * 46m : 800m + (number - 10) * 46m;
                row.Seats.Add(Seat(number.ToString(), number, x, rowNumber == 12 ? 100m : 165m, 0));
            }
            upper.Rows.Add(row);
        }
        template.Sections.Add(upper); return template;
    }

    private static SeatingTemplate CreateKamertal(int locationId, DateTimeOffset now)
    {
        var template = Base(locationId, "Teatri Kamertal AAB", 1200, 800, now);
        template.StageLabel = "STAGE"; template.StageX = 300; template.StageY = 285; template.StageWidth = 600; template.StageHeight = 350;
        var b = new SeatingTemplateSection { Name = "B", DisplayOrder = 1 };
        AddHorizontal(b, "B1", 1, 1, 15, 330, 190, 38, 0);
        AddHorizontal(b, "B2", 2, 16, 29, 380, 138, 38, 0);
        AddHorizontal(b, "B3", 3, 30, 43, 410, 86, 38, 0);
        template.Sections.Add(b);
        var a = new SeatingTemplateSection { Name = "A", DisplayOrder = 2 };
        AddVerticalDescending(a, "A1", 1, 2, 10, 220, 320, 38, -90);
        AddVerticalDescending(a, "A2", 2, 12, 21, 158, 285, 38, -90);
        AddVerticalDescending(a, "A3", 3, 22, 32, 96, 250, 38, -90);
        template.Sections.Add(a);
        var c = new SeatingTemplateSection { Name = "C", DisplayOrder = 3 };
        AddVertical(c, "C1", 1, 1, 10, 980, 285, 38, 90);
        AddVertical(c, "C2", 2, 11, 20, 1042, 250, 38, 90);
        AddVertical(c, "C3", 3, 21, 30, 1104, 250, 38, 90);
        template.Sections.Add(c); return template;
    }

    private static SeatingTemplate Base(int locationId, string name, decimal width, decimal height, DateTimeOffset now) => new() { LocationId = locationId, Name = name, IsDefault = true, IsActive = true, CanvasWidth = width, CanvasHeight = height, CreatedAt = now, UpdatedAt = now };
    private static SeatingTemplateSeat Seat(string label, int order, decimal x, decimal y, decimal rotation) => new() { Label = label, DisplayOrder = order, PositionX = x, PositionY = y, Rotation = rotation, IsActive = true };
    private static void AddHorizontal(SeatingTemplateSection section, string label, int order, int first, int last, decimal x, decimal y, decimal gap, decimal rotation) { var row = new SeatingTemplateRow { Label = label, DisplayOrder = order }; for (var n = first; n <= last; n++) row.Seats.Add(Seat(n.ToString().PadLeft(2, '0'), n, x + (n - first) * gap, y, rotation)); section.Rows.Add(row); }
    private static void AddVertical(SeatingTemplateSection section, string label, int order, int first, int last, decimal x, decimal y, decimal gap, decimal rotation) { var row = new SeatingTemplateRow { Label = label, DisplayOrder = order }; for (var n = first; n <= last; n++) row.Seats.Add(Seat(n.ToString().PadLeft(2, '0'), n, x, y + (n - first) * gap, rotation)); section.Rows.Add(row); }
    private static void AddVerticalDescending(SeatingTemplateSection section, string label, int order, int first, int last, decimal x, decimal y, decimal gap, decimal rotation) { var row = new SeatingTemplateRow { Label = label, DisplayOrder = order }; for (var offset = 0; offset <= last - first; offset++) { var number = last - offset; row.Seats.Add(Seat(number.ToString().PadLeft(2, '0'), offset + 1, x, y + offset * gap, rotation)); } section.Rows.Add(row); }
}

public sealed class SeatingTemplateInitializer(IServiceScopeFactory scopes, ILogger<SeatingTemplateInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken token)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            var locations = await db.Locations
                .Include(x => x.Translations)
                .Include(x => x.SeatingTemplates).ThenInclude(x => x.Sections).ThenInclude(x => x.Rows).ThenInclude(x => x.Seats)
                .ToListAsync(token);
            foreach (var location in locations)
            {
                var venue = location.Translations.FirstOrDefault(x => x.LanguageId == 1)?.Name ?? location.Translations.FirstOrDefault()?.Name ?? "";
                var key = DefaultSeatingLayouts.Identify(venue, ""); if (key is null) continue;
                var existing = location.SeatingTemplates.FirstOrDefault(x => DefaultSeatingLayouts.Identify(venue, x.Name) == key);
                if (key == DefaultSeatingLayouts.FarukKey && existing is not null
                    && (existing.CanvasWidth < 1300 || existing.CanvasHeight < 950 || existing.StageY < 900
                        || existing.Sections.SelectMany(x => x.Rows).Any(x => int.TryParse(x.Label, out _))
                        || await db.PerformanceSeats.AnyAsync(x => x.Layout.SourceTemplateId == existing.Id && x.RowLabel == "1", token)))
                {
                    var refreshed = DefaultSeatingLayouts.Create(key, location.Id, DateTimeOffset.UtcNow);
                    var copiedLayouts = await db.PerformanceSeatingLayouts
                        .Include(x => x.Seats).ThenInclude(x => x.Allocations)
                        .Include(x => x.Seats).ThenInclude(x => x.Holds)
                        .Where(x => x.SourceTemplateId == existing.Id)
                        .ToListAsync(token);
                    foreach (var layout in copiedLayouts)
                    {
                        var refreshedSeats = refreshed.Sections.SelectMany(section => section.Rows.SelectMany(row => row.Seats.Select(seat => new { Section = section.Name, Row = row.Label, Seat = seat })))
                            .ToDictionary(x => $"{x.Section}|{x.Row}|{x.Seat.Label}");
                        var canUpdateInPlace = layout.Seats.Count == refreshedSeats.Count && layout.Seats.All(x => refreshedSeats.ContainsKey($"{x.SectionName}|{x.RowLabel}|{x.SeatLabel}"));
                        if (canUpdateInPlace)
                        {
                            foreach (var seat in layout.Seats)
                            {
                                var source = refreshedSeats[$"{seat.SectionName}|{seat.RowLabel}|{seat.SeatLabel}"].Seat;
                                seat.PositionX = source.PositionX; seat.PositionY = source.PositionY; seat.Rotation = source.Rotation;
                            }
                            layout.CanvasWidth = refreshed.CanvasWidth; layout.CanvasHeight = refreshed.CanvasHeight;
                            layout.StageLabel = refreshed.StageLabel; layout.StageX = refreshed.StageX; layout.StageY = refreshed.StageY;
                            layout.StageWidth = refreshed.StageWidth; layout.StageHeight = refreshed.StageHeight; layout.UpdatedAt = DateTimeOffset.UtcNow;
                            continue;
                        }
                        var currentRows = layout.Seats.GroupBy(x => new { x.SectionOrder, x.RowOrder }).OrderBy(x => x.Key.SectionOrder).ThenBy(x => x.Key.RowOrder).ToList();
                        var targetRows = refreshed.Sections.OrderBy(x => x.DisplayOrder).SelectMany(section => section.Rows.OrderBy(x => x.DisplayOrder).Select(row => new { Section = section, Row = row, Seats = row.Seats.OrderBy(x => x.DisplayOrder).ToList() })).ToList();
                        var canMapByOrder = currentRows.Count == targetRows.Count && currentRows.Zip(targetRows).All(pair => pair.First.Count() == pair.Second.Seats.Count);
                        if (canMapByOrder)
                        {
                            foreach (var pair in currentRows.Zip(targetRows))
                            foreach (var mapped in pair.First.OrderBy(x => x.SeatOrder).Zip(pair.Second.Seats))
                            {
                                mapped.First.SectionName = pair.Second.Section.Name; mapped.First.RowLabel = pair.Second.Row.Label;
                                mapped.First.SectionOrder = pair.Second.Section.DisplayOrder; mapped.First.RowOrder = pair.Second.Row.DisplayOrder;
                                mapped.First.SeatLabel = mapped.Second.Label; mapped.First.SeatOrder = mapped.Second.DisplayOrder;
                                mapped.First.PositionX = mapped.Second.PositionX; mapped.First.PositionY = mapped.Second.PositionY;
                                mapped.First.Rotation = mapped.Second.Rotation;
                            }
                            layout.CanvasWidth = refreshed.CanvasWidth; layout.CanvasHeight = refreshed.CanvasHeight;
                            layout.StageLabel = refreshed.StageLabel; layout.StageX = refreshed.StageX; layout.StageY = refreshed.StageY;
                            layout.StageWidth = refreshed.StageWidth; layout.StageHeight = refreshed.StageHeight; layout.UpdatedAt = DateTimeOffset.UtcNow;
                            continue;
                        }
                        if (layout.Seats.SelectMany(x => x.Allocations).Any() || layout.Seats.SelectMany(x => x.Holds).Any(x => x.ExpiresAt > DateTimeOffset.UtcNow)) continue;
                        db.PerformanceSeatHolds.RemoveRange(layout.Seats.SelectMany(x => x.Holds));
                        db.PerformanceSeats.RemoveRange(layout.Seats);
                        layout.Seats.Clear();
                        layout.CanvasWidth = refreshed.CanvasWidth; layout.CanvasHeight = refreshed.CanvasHeight;
                        layout.StageLabel = refreshed.StageLabel; layout.StageX = refreshed.StageX; layout.StageY = refreshed.StageY;
                        layout.StageWidth = refreshed.StageWidth; layout.StageHeight = refreshed.StageHeight;
                        foreach (var section in refreshed.Sections)
                        foreach (var row in section.Rows)
                        foreach (var seat in row.Seats)
                            layout.Seats.Add(new PerformanceSeat { SectionName = section.Name, RowLabel = row.Label, SeatLabel = seat.Label, SectionOrder = section.DisplayOrder, RowOrder = row.DisplayOrder, SeatOrder = seat.DisplayOrder, PositionX = seat.PositionX, PositionY = seat.PositionY, Rotation = seat.Rotation, IsActive = seat.IsActive });
                        layout.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    db.SeatingTemplateSections.RemoveRange(existing.Sections);
                    existing.Sections.Clear();
                    existing.CanvasWidth = refreshed.CanvasWidth; existing.CanvasHeight = refreshed.CanvasHeight;
                    existing.StageLabel = refreshed.StageLabel; existing.StageX = refreshed.StageX; existing.StageY = refreshed.StageY;
                    existing.StageWidth = refreshed.StageWidth; existing.StageHeight = refreshed.StageHeight;
                    foreach (var section in refreshed.Sections) existing.Sections.Add(section);
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                }
                if (key == DefaultSeatingLayouts.KamertalKey && existing is not null)
                {
                    var corrected = false;
                    foreach (var section in existing.Sections)
                    foreach (var seat in section.Rows.SelectMany(x => x.Seats))
                    {
                        if (section.Name.Equals("A", StringComparison.OrdinalIgnoreCase) && seat.Rotation == 90) { seat.Rotation = -90; corrected = true; }
                        if (section.Name.Equals("C", StringComparison.OrdinalIgnoreCase) && seat.Rotation == -90) { seat.Rotation = 90; corrected = true; }
                    }
                    var sectionA = existing.Sections.FirstOrDefault(x => x.Name.Equals("A", StringComparison.OrdinalIgnoreCase));
                    if (sectionA is not null)
                    foreach (var row in sectionA.Rows)
                    {
                        var maximum = row.Label.ToUpperInvariant() switch { "A1" => 10, "A2" => 21, "A3" => 32, _ => 0 };
                        if (maximum == 0) continue;
                        var ordered = row.Seats.OrderBy(x => x.PositionY).ToList();
                        for (var index = 0; index < ordered.Count; index++)
                        {
                            var expected = (maximum - index).ToString().PadLeft(2, '0');
                            if (ordered[index].Label != expected) { ordered[index].Label = expected; corrected = true; }
                        }
                    }
                    if (corrected) existing.UpdatedAt = DateTimeOffset.UtcNow;
                }
                if (existing is not null && existing.Sections.Count != 0) continue;
                var canonical = DefaultSeatingLayouts.Create(key, location.Id, DateTimeOffset.UtcNow);
                if (existing is null) db.SeatingTemplates.Add(canonical);
                else
                {
                    existing.Name = canonical.Name; existing.IsDefault = true; existing.IsActive = true; existing.CanvasWidth = canonical.CanvasWidth; existing.CanvasHeight = canonical.CanvasHeight;
                    existing.StageLabel = canonical.StageLabel; existing.StageX = canonical.StageX; existing.StageY = canonical.StageY; existing.StageWidth = canonical.StageWidth; existing.StageHeight = canonical.StageHeight; existing.UpdatedAt = DateTimeOffset.UtcNow;
                    foreach (var section in canonical.Sections) existing.Sections.Add(section);
                }
            }
            await db.SaveChangesAsync(token);
        }
        catch (Exception ex) { logger.LogError(ex, "Could not initialize default seating templates."); }
    }
    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
