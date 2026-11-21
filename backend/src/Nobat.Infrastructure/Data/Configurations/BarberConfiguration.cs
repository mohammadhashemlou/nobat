using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class BarberConfiguration : IEntityTypeConfiguration<Barber>
{
    public void Configure(EntityTypeBuilder<Barber> builder)
    {
        builder.ToTable("Barbers");

        builder.Property(b => b.FullName)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(b => b.Bio)
               .HasMaxLength(1000);

        builder.Property(b => b.AvatarUrl)
               .HasMaxLength(500);

        builder.Property(b => b.IsActive)
               .HasDefaultValue(true);

        builder.HasIndex(b => b.SalonId);

        builder.HasOne(b => b.Salon)
               .WithMany(s => s.Barbers)
               .HasForeignKey(b => b.SalonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
