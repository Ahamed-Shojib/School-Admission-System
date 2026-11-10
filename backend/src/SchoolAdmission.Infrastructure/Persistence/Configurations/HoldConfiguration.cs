using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("holds");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasMaxLength(32);
        builder.Property(h => h.ClassId).HasMaxLength(32).IsRequired();
        builder.Property(h => h.StudentId).HasMaxLength(17).IsRequired();

        // Stored as HELD / CONFIRMED / EXPIRED / CANCELLED so the values are readable in the database.
        builder.Property(h => h.Status)
            .HasMaxLength(16)
            .HasConversion(
                s => s.ToString().ToUpperInvariant(),
                s => Enum.Parse<HoldStatus>(s, true))
            .IsRequired();

        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(h => h.ClassId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.ClassId, h.Status });
        builder.HasIndex(h => h.StudentId).HasDatabaseName("IX_holds_StudentId");

        // Safety net: a student can be CONFIRMED in at most one class.
        builder.HasIndex(h => h.StudentId)
            .IsUnique()
            .HasFilter("\"Status\" = 'CONFIRMED'")
            .HasDatabaseName("IX_holds_StudentId_confirmed");

        // Safety net: at most one live (HELD or CONFIRMED) application per student per class.
        builder.HasIndex(h => new { h.ClassId, h.StudentId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('HELD', 'CONFIRMED')");
    }
}
