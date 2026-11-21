using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");

        builder.Property(h => h.Description)
               .HasMaxLength(500);

        builder.HasOne(h => h.Salon)
               .WithMany(s => s.Holidays)
               .HasForeignKey(h => h.SalonId)
               .OnDelete(DeleteBehavior.Cascade);

        // One holiday per salon per date
        builder.HasIndex(h => new { h.SalonId, h.Date })
               .IsUnique();
    }
}
