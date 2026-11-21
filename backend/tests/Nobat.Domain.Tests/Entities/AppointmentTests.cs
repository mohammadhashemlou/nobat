using Nobat.Domain.Common;
using Nobat.Domain.Entities;
using Nobat.Domain.Enums;

namespace Nobat.Domain.Tests.Entities;

public class AppointmentTests
{
    [Fact]
    public void NewAppointment_DefaultsToPending()
    {
        var appointment = new Appointment();

        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
    }

    [Fact]
    public void NewAppointment_StartsWithEmptyStatusHistory()
    {
        var appointment = new Appointment();

        Assert.NotNull(appointment.StatusHistory);
        Assert.Empty(appointment.StatusHistory);
    }

    [Fact]
    public void StatusHistory_CanRecordTransitions()
    {
        var appointmentId = Guid.NewGuid();
        var appointment = new Appointment { Id = appointmentId };

        appointment.StatusHistory.Add(new AppointmentStatusHistory
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Status = AppointmentStatus.Confirmed
        });
        appointment.StatusHistory.Add(new AppointmentStatusHistory
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            Status = AppointmentStatus.Cancelled
        });

        Assert.Equal(2, appointment.StatusHistory.Count);
        Assert.All(appointment.StatusHistory, h => Assert.Equal(appointmentId, h.AppointmentId));
    }

    [Fact]
    public void CancellationMetadata_IsNullUntilCancelled()
    {
        var appointment = new Appointment();

        Assert.Null(appointment.CancelledAt);

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTime.UtcNow;

        Assert.NotNull(appointment.CancelledAt);
    }
}

public class BaseEntityTests
{
    [Fact]
    public void NewEntity_HasEmptyId()
    {
        var entity = new Service();

        Assert.Equal(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Entities_CanBeIdentifiedById()
    {
        var id = Guid.NewGuid();

        var a = new Service { Id = id };
        var b = new Service { Id = id };

        Assert.Equal(a.Id, b.Id);
    }
}
