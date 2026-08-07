using System.ComponentModel.DataAnnotations;

namespace Theatre.Api.DTOs;

public sealed record SeatDto(int Id, string Section, string Row, string Label, int SectionOrder, int RowOrder, int SeatOrder, decimal X, decimal Y, decimal Rotation, bool IsActive, string State, int? ReservationId, long? AllocationId, string? AllocationComment);
public sealed record PublicSeatingDto(int PerformanceId, string ShowTitle, DateTimeOffset StartsAt, string Venue, decimal CanvasWidth, decimal CanvasHeight, string? StageLabel, decimal? StageX, decimal? StageY, decimal? StageWidth, decimal? StageHeight, bool ReservationsAvailable, int? MaxSeatsPerReservation, string? UnavailableMessage, IReadOnlyList<SeatDto> Seats);
public sealed record CreatePublicReservationRequest([Required, MaxLength(180)] string FullName, [Required] string Phone, string? CountryPrefix, [EmailAddress, MaxLength(254)] string? Email, [MinLength(1)] int[] SeatIds, Guid HoldToken);
public sealed record HoldSeatsRequest([MinLength(1)] int[] SeatIds, Guid? HoldToken);
public sealed record SeatHoldDto(Guid HoldToken, DateTimeOffset ExpiresAt);
public sealed record ReservationResultDto(int Id, string Status, string ConfirmationStatus, DateTimeOffset ReservedAt);
public sealed record SaveCustomerRequest([Required, MaxLength(180)] string FullName, [Required] string Phone, string? CountryPrefix, [EmailAddress, MaxLength(254)] string? Email);
public sealed record AdminReservationRequest(int? CustomerId, SaveCustomerRequest? Customer, [MinLength(1)] int[] SeatIds, string? Comment);
public sealed record AdminBlockSeatsRequest([MinLength(1)] int[] SeatIds, [MaxLength(1000)] string? Comment);
public sealed record UpdateReservationRequest(string? ConfirmationStatus, string? Status, [MaxLength(2000)] string? AdminComment, int[]? SeatIds);
public sealed record ReservationSeatDto(int Id, string Section, string Row, string Label, bool IsActive);
public sealed record ReservationListItemDto(int Id, int CustomerId, string CustomerName, string Phone, string? Email, int PerformanceId, string ShowTitle, DateTimeOffset PerformanceDate, IReadOnlyList<string> Seats, IReadOnlyList<ReservationSeatDto> SeatDetails, IReadOnlyList<int> ActiveSeatIds, DateTimeOffset ReservedAt, string ConfirmationStatus, string Status, string Source, string? AdminComment);
public sealed record ReservationListDto(IReadOnlyList<ReservationListItemDto> Items, int TotalCount, int Page = 1, int PageSize = 25);
public sealed record CustomerHistoryDto(int Id, string FullName, string Phone, string? Email, DateTimeOffset CreatedAt, IReadOnlyList<ReservationListItemDto> Reservations);
public sealed record CustomerListItemDto(int Id, string FullName, string Phone, string? Email, DateTimeOffset CreatedAt, DateTimeOffset? FirstReservationDate, DateTimeOffset? MostRecentReservationDate, int TotalReservations, int TotalReservedSeats, DateTimeOffset? AnonymizedAt);
public sealed record CustomerListDto(IReadOnlyList<CustomerListItemDto> Items, int TotalCount, int Page, int PageSize);
public sealed record CustomerReservationHistoryDto(int ReservationId, string Play, DateTimeOffset PerformanceDate, string Venue, IReadOnlyList<string> Seats, string ConfirmationStatus, string Status, string Source, string? AdminComment, DateTimeOffset ReservedAt);
public sealed record CustomerDetailDto(int Id, string FullName, string Phone, string? Email, DateTimeOffset CreatedAt, DateTimeOffset? AnonymizedAt, IReadOnlyList<CustomerReservationHistoryDto> Reservations, IReadOnlyList<AdminAuditDto> Audit);
public sealed record AdminAuditDto(long Id, int? AdminUserId, string? AdminName, string ActionType, string EntityType, string EntityId, string? PreviousValuesJson, string? NewValuesJson, DateTimeOffset CreatedAt);
public sealed record SaveTemplateSeatRequest(int? Id, [Required, MaxLength(40)] string Label, int DisplayOrder, decimal X, decimal Y, decimal Rotation, bool IsActive = true);
public sealed record SaveTemplateRowRequest([Required, MaxLength(40)] string Label, int DisplayOrder, [MinLength(1)] SaveTemplateSeatRequest[] Seats);
public sealed record SaveTemplateSectionRequest([Required, MaxLength(100)] string Name, int DisplayOrder, [MinLength(1)] SaveTemplateRowRequest[] Rows);
public sealed record SaveSeatingTemplateRequest(int LocationId, [Required, MaxLength(180)] string Name, bool IsDefault, bool IsActive, [Range(100,5000)] decimal CanvasWidth, [Range(100,5000)] decimal CanvasHeight, string? StageLabel, decimal? StageX, decimal? StageY, decimal? StageWidth, decimal? StageHeight, [MinLength(1)] SaveTemplateSectionRequest[] Sections);
public sealed record SeatingTemplateDto(int Id, int LocationId, string Venue, string Name, bool IsDefault, bool IsActive, decimal CanvasWidth, decimal CanvasHeight, string? StageLabel, decimal? StageX, decimal? StageY, decimal? StageWidth, decimal? StageHeight, IReadOnlyList<SaveTemplateSectionRequest> Sections);
public sealed record UpdatePerformanceSeatRequest([Required, MaxLength(100)] string Section, [Required, MaxLength(40)] string Row, [Required, MaxLength(40)] string Label, int SectionOrder, int RowOrder, int SeatOrder, decimal X, decimal Y, decimal Rotation, bool IsActive);
