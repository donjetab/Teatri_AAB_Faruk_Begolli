using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Theatre.Api.Models;

namespace Theatre.Api.Data.Configurations;

internal sealed class SeatingTemplateConfiguration : IEntityTypeConfiguration<SeatingTemplate>
{
    public void Configure(EntityTypeBuilder<SeatingTemplate> b) { b.Property(x => x.Name).HasMaxLength(180).IsRequired(); b.Property(x => x.StageLabel).HasMaxLength(80); foreach (var p in new[] { nameof(SeatingTemplate.CanvasWidth), nameof(SeatingTemplate.CanvasHeight), nameof(SeatingTemplate.StageX), nameof(SeatingTemplate.StageY), nameof(SeatingTemplate.StageWidth), nameof(SeatingTemplate.StageHeight) }) b.Property(p).HasPrecision(9,2); b.HasIndex(x => new { x.LocationId, x.Name }).IsUnique(); b.HasOne(x => x.Location).WithMany(x => x.SeatingTemplates).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict); }
}
internal sealed class SeatingTemplateSectionConfiguration : IEntityTypeConfiguration<SeatingTemplateSection>
{
    public void Configure(EntityTypeBuilder<SeatingTemplateSection> b) { b.Property(x => x.Name).HasMaxLength(100).IsRequired(); b.HasOne(x => x.SeatingTemplate).WithMany(x => x.Sections).HasForeignKey(x => x.SeatingTemplateId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.SeatingTemplateId, x.DisplayOrder }).IsUnique(); }
}
internal sealed class SeatingTemplateRowConfiguration : IEntityTypeConfiguration<SeatingTemplateRow>
{
    public void Configure(EntityTypeBuilder<SeatingTemplateRow> b) { b.Property(x => x.Label).HasMaxLength(40).IsRequired(); b.HasOne(x => x.Section).WithMany(x => x.Rows).HasForeignKey(x => x.SeatingTemplateSectionId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.SeatingTemplateSectionId, x.DisplayOrder }).IsUnique(); }
}
internal sealed class SeatingTemplateSeatConfiguration : IEntityTypeConfiguration<SeatingTemplateSeat>
{
    public void Configure(EntityTypeBuilder<SeatingTemplateSeat> b) { b.Property(x => x.Label).HasMaxLength(40).IsRequired(); b.Property(x => x.PositionX).HasPrecision(9,2); b.Property(x => x.PositionY).HasPrecision(9,2); b.Property(x => x.Rotation).HasPrecision(6,2); b.HasOne(x => x.Row).WithMany(x => x.Seats).HasForeignKey(x => x.SeatingTemplateRowId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.SeatingTemplateRowId, x.DisplayOrder }).IsUnique(); }
}
internal sealed class PerformanceSeatingLayoutConfiguration : IEntityTypeConfiguration<PerformanceSeatingLayout>
{
    public void Configure(EntityTypeBuilder<PerformanceSeatingLayout> b) { b.Property(x => x.StageLabel).HasMaxLength(80); foreach (var p in new[] { nameof(PerformanceSeatingLayout.CanvasWidth), nameof(PerformanceSeatingLayout.CanvasHeight), nameof(PerformanceSeatingLayout.StageX), nameof(PerformanceSeatingLayout.StageY), nameof(PerformanceSeatingLayout.StageWidth), nameof(PerformanceSeatingLayout.StageHeight) }) b.Property(p).HasPrecision(9,2); b.HasIndex(x => x.PerformanceId).IsUnique(); b.HasOne(x => x.Performance).WithOne(x => x.SeatingLayout).HasForeignKey<PerformanceSeatingLayout>(x => x.PerformanceId).OnDelete(DeleteBehavior.Cascade); }
}
internal sealed class PerformanceSeatConfiguration : IEntityTypeConfiguration<PerformanceSeat>
{
    public void Configure(EntityTypeBuilder<PerformanceSeat> b) { b.Property(x => x.SectionName).HasMaxLength(100).IsRequired(); b.Property(x => x.RowLabel).HasMaxLength(40).IsRequired(); b.Property(x => x.SeatLabel).HasMaxLength(40).IsRequired(); b.Property(x => x.PositionX).HasPrecision(9,2); b.Property(x => x.PositionY).HasPrecision(9,2); b.Property(x => x.Rotation).HasPrecision(6,2); b.HasOne(x => x.Layout).WithMany(x => x.Seats).HasForeignKey(x => x.PerformanceSeatingLayoutId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.PerformanceSeatingLayoutId, x.SectionName, x.RowLabel, x.SeatLabel }).IsUnique(); }
}
internal sealed class PerformanceSeatHoldConfiguration : IEntityTypeConfiguration<PerformanceSeatHold>
{
    public void Configure(EntityTypeBuilder<PerformanceSeatHold> b) { b.HasKey(x => x.Id); b.HasOne(x => x.PerformanceSeat).WithMany(x => x.Holds).HasForeignKey(x => x.PerformanceSeatId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => x.PerformanceSeatId).IsUnique(); b.HasIndex(x => x.HoldToken); b.HasIndex(x => x.ExpiresAt); }
}
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b) { b.Property(x => x.FullName).HasMaxLength(180).IsRequired(); b.Property(x => x.NormalizedPhone).HasMaxLength(24).IsRequired(); b.Property(x => x.Email).HasMaxLength(254); b.HasIndex(x => x.NormalizedPhone).IsUnique(); }
}
internal sealed class ReservationAdminAuditConfiguration : IEntityTypeConfiguration<ReservationAdminAudit>
{
    public void Configure(EntityTypeBuilder<ReservationAdminAudit> b)
    {
        b.Property(x => x.ActionType).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityId).HasMaxLength(80).IsRequired();
        b.Property(x => x.PreviousValuesJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        b.HasOne(x => x.AdminUser).WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Reservation).WithMany().HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAt });
        b.HasIndex(x => x.ReservationId);
        b.HasIndex(x => x.CustomerId);
    }
}
internal sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b) { b.Property(x => x.ConfirmationStatus).HasConversion<string>().HasMaxLength(24); b.Property(x => x.Status).HasConversion<string>().HasMaxLength(24); b.Property(x => x.Source).HasConversion<string>().HasMaxLength(24); b.Property(x => x.AdminComment).HasMaxLength(2000); b.HasOne(x => x.Customer).WithMany(x => x.Reservations).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Performance).WithMany(x => x.Reservations).HasForeignKey(x => x.PerformanceId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.PerformanceId, x.Status }); }
}
internal sealed class SeatAllocationConfiguration : IEntityTypeConfiguration<SeatAllocation>
{
    public void Configure(EntityTypeBuilder<SeatAllocation> b) { b.Property(x => x.Type).HasConversion<string>().HasMaxLength(24); b.Property(x => x.AdminComment).HasMaxLength(1000); b.HasOne(x => x.PerformanceSeat).WithMany(x => x.Allocations).HasForeignKey(x => x.PerformanceSeatId).OnDelete(DeleteBehavior.Restrict); b.HasOne(x => x.Reservation).WithMany(x => x.SeatAllocations).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.PerformanceSeatId).IsUnique().HasFilter("[IsActive] = 1"); b.ToTable(t => t.HasCheckConstraint("CK_SeatAllocation_Owner", "([Type] = 'AdminBlock' AND [ReservationId] IS NULL) OR ([Type] = 'Reservation' AND [ReservationId] IS NOT NULL)")); }
}
internal sealed class ReservationAuditEventConfiguration : IEntityTypeConfiguration<ReservationAuditEvent>
{
    public void Configure(EntityTypeBuilder<ReservationAuditEvent> b) { b.Property(x => x.EventType).HasMaxLength(80).IsRequired(); b.Property(x => x.Details).HasMaxLength(2000); b.HasOne(x => x.Reservation).WithMany(x => x.AuditEvents).HasForeignKey(x => x.ReservationId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.ReservationId, x.CreatedAt }); }
}
