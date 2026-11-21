using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class SalonConfiguration : IEntityTypeConfiguration<Salon>
{
    public void Configure(EntityTypeBuilder<Salon> builder)
    {
        builder.ToTable("Salons");

        builder.Property(s => s.Name)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(s => s.Slug)
               .HasMaxLength(250)
               .IsRequired();

        builder.HasIndex(s => s.Slug)
               .IsUnique();

        builder.Property(s => s.Address)
               .HasMaxLength(500);

        builder.Property(s => s.Phone)
               .HasMaxLength(20);

        builder.Property(s => s.Description)
               .HasMaxLength(2000);

        builder.Property(s => s.DefaultTimezone)
               .HasMaxLength(50)
               .HasDefaultValue("Asia/Tehran");
    }
}
