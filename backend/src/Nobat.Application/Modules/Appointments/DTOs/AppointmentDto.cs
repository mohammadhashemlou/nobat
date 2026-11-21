namespace Nobat.Application.Modules.Appointments.DTOs;

public record AppointmentDto
{
    public Guid Id { get; init; }
    public Guid SalonId { get; init; }
    public Guid BarberId { get; init; }
    public string BarberName { get; init; } = string.Empty;
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public decimal Price { get; init; }
    public int DurationMinutes { get; init; }
}
