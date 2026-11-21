using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");

        builder.Property(s => s.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(s => s.Description)
               .HasMaxLength(1000);

        builder.Property(s => s.DurationMinutes)
               .IsRequired();

        builder.Property(s => s.Price)
               .HasColumnType("numeric(18,2)")
               .IsRequired();

        builder.Property(s => s.IsActive)
               .HasDefaultValue(true);

        builder.HasIndex(s => s.SalonId);

        builder.HasOne(s => s.Salon)
               .WithMany(s => s.Services)
               .HasForeignKey(s => s.SalonId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
