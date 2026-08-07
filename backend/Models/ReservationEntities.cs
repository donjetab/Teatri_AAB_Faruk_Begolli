namespace Theatre.Api.Models;

public sealed class SeatingTemplate
{
    public int Id { get; set; }
    public int LocationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal CanvasWidth { get; set; } = 1000;
    public decimal CanvasHeight { get; set; } = 800;
    public string? StageLabel { get; set; }
    public decimal? StageX { get; set; }
    public decimal? StageY { get; set; }
    public decimal? StageWidth { get; set; }
    public decimal? StageHeight { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Location Location { get; set; } = null!;
    public ICollection<SeatingTemplateSection> Sections { get; set; } = [];
    public ICollection<ShowPerformance> Performances { get; set; } = [];
}

public sealed class SeatingTemplateSection
{
    public int Id { get; set; }
    public int SeatingTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public SeatingTemplate SeatingTemplate { get; set; } = null!;
    public ICollection<SeatingTemplateRow> Rows { get; set; } = [];
}

public sealed class SeatingTemplateRow
{
    public int Id { get; set; }
    public int SeatingTemplateSectionId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public SeatingTemplateSection Section { get; set; } = null!;
    public ICollection<SeatingTemplateSeat> Seats { get; set; } = [];
}

public sealed class SeatingTemplateSeat
{
    public int Id { get; set; }
    public int SeatingTemplateRowId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Rotation { get; set; }
    public SeatingTemplateRow Row { get; set; } = null!;
}

public sealed class PerformanceSeatingLayout
{
    public int Id { get; set; }
    public int PerformanceId { get; set; }
    public int? SourceTemplateId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public decimal CanvasWidth { get; set; } = 1000;
    public decimal CanvasHeight { get; set; } = 800;
    public string? StageLabel { get; set; }
    public decimal? StageX { get; set; }
    public decimal? StageY { get; set; }
    public decimal? StageWidth { get; set; }
    public decimal? StageHeight { get; set; }
    public ShowPerformance Performance { get; set; } = null!;
    public ICollection<PerformanceSeat> Seats { get; set; } = [];
}

public sealed class PerformanceSeat
{
    public int Id { get; set; }
    public int PerformanceSeatingLayoutId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string RowLabel { get; set; } = string.Empty;
    public string SeatLabel { get; set; } = string.Empty;
    public int SectionOrder { get; set; }
    public int RowOrder { get; set; }
    public int SeatOrder { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal Rotation { get; set; }
    public PerformanceSeatingLayout Layout { get; set; } = null!;
    public ICollection<SeatAllocation> Allocations { get; set; } = [];
    public ICollection<PerformanceSeatHold> Holds { get; set; } = [];
}

public sealed class PerformanceSeatHold
{
    public Guid Id { get; set; }
    public Guid HoldToken { get; set; }
    public int PerformanceSeatId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public PerformanceSeat PerformanceSeat { get; set; } = null!;
}

public sealed class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string NormalizedPhone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? AnonymizedAt { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = [];
}

public sealed class ReservationAdminAudit
{
    public long Id { get; set; }
    public int? AdminUserId { get; set; }
    public int? ReservationId { get; set; }
    public int? CustomerId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? PreviousValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AdminUser? AdminUser { get; set; }
    public Reservation? Reservation { get; set; }
    public Customer? Customer { get; set; }
}

public sealed class Reservation
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int PerformanceId { get; set; }
    public DateTimeOffset ReservedAt { get; set; }
    public ConfirmationStatus ConfirmationStatus { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public ReservationSource Source { get; set; }
    public string? AdminComment { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Customer Customer { get; set; } = null!;
    public ShowPerformance Performance { get; set; } = null!;
    public ICollection<SeatAllocation> SeatAllocations { get; set; } = [];
    public ICollection<ReservationAuditEvent> AuditEvents { get; set; } = [];
}

public sealed class SeatAllocation
{
    public long Id { get; set; }
    public int PerformanceSeatId { get; set; }
    public int? ReservationId { get; set; }
    public SeatAllocationType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AdminComment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
    public PerformanceSeat PerformanceSeat { get; set; } = null!;
    public Reservation? Reservation { get; set; }
}

public sealed class ReservationAuditEvent
{
    public long Id { get; set; }
    public int ReservationId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Details { get; set; }
    public int? AdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Reservation Reservation { get; set; } = null!;
}
