using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nobat.Domain.Entities;
using Nobat.Infrastructure.Data.Configurations;

namespace Nobat.Infrastructure.Data;

public class NobatDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public NobatDbContext(DbContextOptions<NobatDbContext> options) : base(options)
    {
    }

    public DbSet<Salon> Salons => Set<Salon>();
    public DbSet<Barber> Barbers => Set<Barber>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BarberService> BarberServices => Set<BarberService>();
    public DbSet<WorkingHours> WorkingHours => Set<WorkingHours>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<AppointmentStatusHistory> AppointmentStatusHistory => Set<AppointmentStatusHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(NobatDbContext).Assembly);
    }
}
