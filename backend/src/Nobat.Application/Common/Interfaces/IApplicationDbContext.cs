using Microsoft.EntityFrameworkCore;
using Nobat.Domain.Entities;

namespace Nobat.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext so the Application layer
/// does not depend on Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Salon> Salons { get; }
    DbSet<Barber> Barbers { get; }
    DbSet<Service> Services { get; }
    DbSet<BarberService> BarberServices { get; }
    DbSet<WorkingHours> WorkingHours { get; }
    DbSet<Holiday> Holidays { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<AppointmentStatusHistory> AppointmentStatusHistory { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
