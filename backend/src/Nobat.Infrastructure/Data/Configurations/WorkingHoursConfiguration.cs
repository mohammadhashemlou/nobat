using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class WorkingHoursConfiguration : IEntityTypeConfiguration<WorkingHours>
{
    public void Configure(EntityTypeBuilder<WorkingHours> builder)
    {
        builder.ToTable("WorkingHours");

        builder.Property(w => w.DayOfWeek)
               .HasConversion<int>();

        builder.Property(w => w.StartTime)
               .IsRequired();

        builder.Property(w => w.EndTime)
               .IsRequired();

        builder.HasOne(w => w.Barber)
               .WithMany(b => b.WorkingHours)
               .HasForeignKey(w => w.BarberId)
               .OnDelete(DeleteBehavior.Cascade);

        // One barber can have one working hours entry per day
        builder.HasIndex(w => new { w.BarberId, w.DayOfWeek })
               .IsUnique();
    }
}
