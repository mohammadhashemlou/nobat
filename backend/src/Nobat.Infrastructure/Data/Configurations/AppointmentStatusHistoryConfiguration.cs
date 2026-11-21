using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Infrastructure.Data.Configurations;

public class AppointmentStatusHistoryConfiguration : IEntityTypeConfiguration<AppointmentStatusHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentStatusHistory> builder)
    {
        builder.ToTable("AppointmentStatusHistory");

        builder.Property(h => h.Status)
               .HasConversion<int>();

        builder.HasOne(h => h.Appointment)
               .WithMany(a => a.StatusHistory)
               .HasForeignKey(h => h.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.AppointmentId);
    }
}
