using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nobat.Domain.Entities;

namespace Nobat.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
               .HasMaxLength(150)
               .IsRequired();

        builder.Property(u => u.Role)
               .HasConversion<int>();

        builder.Property(u => u.IsActive)
               .HasDefaultValue(true);

        builder.HasIndex(u => u.FullName);

        // Rename Identity tables
        builder.ToTable("Users");
    }
}
