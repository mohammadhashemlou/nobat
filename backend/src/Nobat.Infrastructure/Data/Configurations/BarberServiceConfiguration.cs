using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class BarberServiceConfiguration : IEntityTypeConfiguration<BarberService>
{
    public void Configure(EntityTypeBuilder<BarberService> builder)
    {
        builder.ToTable("BarberServices");

        builder.HasKey(bs => new { bs.BarberId, bs.ServiceId });

        builder.HasOne(bs => bs.Barber)
               .WithMany(b => b.BarberServices)
               .HasForeignKey(bs => bs.BarberId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bs => bs.Service)
               .WithMany(s => s.BarberServices)
               .HasForeignKey(bs => bs.ServiceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
