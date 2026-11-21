using FluentValidation;
using MediatR;
using Nobat.Application.Common.Behaviors;
using AppValidationException = Nobat.Application.Common.Exceptions.ValidationException;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;
using Nobat.Application.Modules.Identity.Validators;
using Nobat.Domain.Enums;

namespace Nobat.Application.Tests.Common;

public class ValidationBehaviorTests
{
    private static RegisterCommand Valid() => new()
    {
        FullName = "علی رضایی",
        Email = "ali@example.com",
        Password = "Passw0rd",
        ConfirmPassword = "Passw0rd",
        Role = UserRole.Customer
    };

    private static ValidationBehavior<RegisterCommand, RegisterResponse> Build(params IValidator<RegisterCommand>[] validators)
        => new(validators);

    [Fact]
    public async Task CallsHandler_WhenNoValidatorsAreRegistered()
    {
        var behavior = Build();
        var called = false;

        var result = await behavior.Handle(Valid(), _ =>
        {
            called = true;
            return Task.FromResult(new RegisterResponse { Email = "handled" });
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("handled", result.Email);
    }

    [Fact]
    public async Task CallsHandler_WhenValidationPasses()
    {
        var behavior = Build(new RegisterCommandValidator());
        var called = false;

        var result = await behavior.Handle(Valid(), _ =>
        {
            called = true;
            return Task.FromResult(new RegisterResponse { Email = "handled" });
        }, CancellationToken.None);

        Assert.True(called);
        Assert.Equal("handled", result.Email);
    }

    [Fact]
    public async Task ThrowsValidationException_AndSkipsHandler_WhenValidationFails()
    {
        var behavior = Build(new RegisterCommandValidator());
        var called = false;

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(Valid() with { Password = "weak", ConfirmPassword = "weak" }, _ =>
            {
                called = true;
                return Task.FromResult(new RegisterResponse { Email = "handled" });
            }, CancellationToken.None));

        Assert.False(called);
        Assert.True(ex.Errors.ContainsKey(nameof(RegisterCommand.Password)));
    }

    [Fact]
    public async Task GroupsErrorsByPropertyName()
    {
        var behavior = Build(new RegisterCommandValidator());

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(
                Valid() with { Email = "bad", Password = "weak", ConfirmPassword = "weak", FullName = "" },
                _ => Task.FromResult(new RegisterResponse { Email = "handled" }),
                CancellationToken.None));

        Assert.Contains(nameof(RegisterCommand.Email), ex.Errors.Keys);
        Assert.Contains(nameof(RegisterCommand.Password), ex.Errors.Keys);
        Assert.Contains(nameof(RegisterCommand.FullName), ex.Errors.Keys);
        Assert.All(ex.Errors.Values, messages => Assert.NotEmpty(messages));
    }

    [Fact]
    public async Task MergesFailures_FromMultipleValidators()
    {
        var behavior = Build(new RegisterCommandValidator(), new ExtraValidator());

        var ex = await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(Valid(), _ => Task.FromResult(new RegisterResponse { Email = "handled" }), CancellationToken.None));

        Assert.Contains(nameof(RegisterCommand.PhoneNumber), ex.Errors.Keys);
    }

    private sealed class ExtraValidator : AbstractValidator<RegisterCommand>
    {
        public ExtraValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .Must(_ => false)
                .WithMessage("خطای اعتبارسنج دوم");
        }
    }
}
