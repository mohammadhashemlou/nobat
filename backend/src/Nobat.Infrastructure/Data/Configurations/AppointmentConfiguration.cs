using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Infrastructure.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(a => a.Status)
               .HasConversion<int>()
               .HasDefaultValue(AppointmentStatus.Pending);

        builder.Property(a => a.Notes)
               .HasMaxLength(1000);

        // Row version for optimistic concurrency
        builder.Property(a => a.RowVersion)
               .IsRowVersion();

        builder.Property(a => a.StartAt)
               .IsRequired();

        builder.Property(a => a.EndAt)
               .IsRequired();

        builder.HasIndex(a => new { a.BarberId, a.StartAt, a.EndAt });

        builder.HasOne(a => a.Salon)
               .WithMany(s => s.Appointments)
               .HasForeignKey(a => a.SalonId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Barber)
               .WithMany(b => b.Appointments)
               .HasForeignKey(a => a.BarberId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Service)
               .WithMany()
               .HasForeignKey(a => a.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Customer)
               .WithMany()
               .HasForeignKey(a => a.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
