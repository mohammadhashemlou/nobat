using FluentValidation.TestHelper;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.Validators;
using Nobat.Domain.Enums;

namespace Nobat.Application.Tests.Identity;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    private static RegisterCommand Valid() => new()
    {
        FullName = "علی رضایی",
        Email = "ali@example.com",
        Password = "Passw0rd",
        ConfirmPassword = "Passw0rd",
        PhoneNumber = "09121234567",
        Role = UserRole.Customer
    };

    [Fact]
    public void Accepts_ValidCustomerRegistration()
    {
        var result = _sut.TestValidate(Valid());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Accepts_RegistrationWithoutPhoneNumber()
    {
        var command = Valid() with { PhoneNumber = null };

        _sut.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_EmptyFullName()
    {
        var result = _sut.TestValidate(Valid() with { FullName = "" });

        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Rejects_OverlongFullName()
    {
        var result = _sut.TestValidate(Valid() with { FullName = new string('a', 151) });

        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("ali@")]
    public void Rejects_InvalidEmail(string email)
    {
        var result = _sut.TestValidate(Valid() with { Email = email });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("", "رمز عبور الزامی است")]
    [InlineData("Ab1", "رمز عبور باید حداقل ۸ کاراکتر باشد")]
    [InlineData("passw0rd", "رمز عبور باید حداقل یک حرف بزرگ داشته باشد")]
    [InlineData("PASSW0RD", "رمز عبور باید حداقل یک حرف کوچک داشته باشد")]
    [InlineData("Password", "رمز عبور باید حداقل یک عدد داشته باشد")]
    public void Rejects_WeakPasswords(string password, string expectedMessage)
    {
        var result = _sut.TestValidate(Valid() with
        {
            Password = password,
            ConfirmPassword = password
        });

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage(expectedMessage);
    }

    [Fact]
    public void Rejects_MismatchedPasswordConfirmation()
    {
        var result = _sut.TestValidate(Valid() with { ConfirmPassword = "Different1" });

        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Theory]
    [InlineData("0912")]
    [InlineData("9121234567")]
    [InlineData("091212345678")]
    [InlineData("0912-123-4567")]
    public void Rejects_MalformedPhoneNumber(string phone)
    {
        var result = _sut.TestValidate(Valid() with { PhoneNumber = phone });

        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Rejects_AdminRoleSelfRegistration()
    {
        var result = _sut.TestValidate(Valid() with { Role = UserRole.Admin });

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }

    [Fact]
    public void Rejects_UndefinedRole()
    {
        var result = _sut.TestValidate(Valid() with { Role = (UserRole)99 });

        result.ShouldHaveValidationErrorFor(x => x.Role);
    }
}

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Accepts_ValidCredentials()
    {
        var result = _sut.TestValidate(new LoginCommand { Email = "a@b.com", Password = "Passw0rd" });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Rejects_EmptyEmail()
    {
        var result = _sut.TestValidate(new LoginCommand { Email = "", Password = "Passw0rd" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Rejects_EmptyPassword()
    {
        var result = _sut.TestValidate(new LoginCommand { Email = "a@b.com", Password = "" });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
