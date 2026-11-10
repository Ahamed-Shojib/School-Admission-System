using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(32);
        builder.Property(c => c.Name).HasMaxLength(SchoolClass.MaxNameLength).IsRequired();
        builder.Property(c => c.Shift).HasMaxLength(32);
        builder.Property(c => c.TotalSeats).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
    }
}
