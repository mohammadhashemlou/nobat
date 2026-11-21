using FluentValidation.TestHelper;
using Nobat.Application.Modules.Appointments.Commands;

namespace Nobat.Application.Tests.Appointments;

public class CreateAppointmentCommandValidatorTests
{
    private readonly CreateAppointmentCommandValidator _sut = new();

    private static CreateAppointmentCommand Valid() => new()
    {
        BarberId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid(),
        StartAtUtc = new DateTime(2026, 11, 23, 6, 30, 0, DateTimeKind.Utc),
        Notes = "کوتاه"
    };

    [Fact]
    public void Accepts_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_EmptyBarberId()
    {
        var result = _sut.TestValidate(Valid() with { BarberId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.BarberId)
            .WithErrorMessage("آرایشگر را انتخاب کنید");
    }

    [Fact]
    public void Rejects_EmptyServiceId()
    {
        var result = _sut.TestValidate(Valid() with { ServiceId = Guid.Empty });

        result.ShouldHaveValidationErrorFor(x => x.ServiceId)
            .WithErrorMessage("خدمت را انتخاب کنید");
    }

    [Theory]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void Rejects_NonUtcStartTime(DateTimeKind kind)
    {
        var start = new DateTime(2026, 11, 23, 9, 0, 0, kind);

        var result = _sut.TestValidate(Valid() with { StartAtUtc = start });

        result.ShouldHaveValidationErrorFor(x => x.StartAtUtc)
            .WithErrorMessage("زمان باید به صورت UTC ارسال شود");
    }

    [Fact]
    public void Rejects_DefaultStartTime()
    {
        var result = _sut.TestValidate(Valid() with { StartAtUtc = default });

        result.ShouldHaveValidationErrorFor(x => x.StartAtUtc);
    }

    [Fact]
    public void Rejects_OverlongNotes()
    {
        var result = _sut.TestValidate(Valid() with { Notes = new string('ن', 1001) });

        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Accepts_NotesAtMaximumLength()
    {
        var result = _sut.TestValidate(Valid() with { Notes = new string('ن', 1000) });

        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}
