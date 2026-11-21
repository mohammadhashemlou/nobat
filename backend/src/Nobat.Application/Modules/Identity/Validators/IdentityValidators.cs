using FluentValidation;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Identity.Validators;

internal static class PhoneNumberRules
{
    public const string Pattern = @"^09\d{9}$";
    public const string Message = "شماره موبایل باید ۱۱ رقم و با ۰۹ شروع شود";
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("نام و نام خانوادگی الزامی است")
            .MaximumLength(150).WithMessage("نام و نام خانوادگی نمی‌تواند بیشتر از ۱۵۰ کاراکتر باشد");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches(PhoneNumberRules.Pattern).WithMessage(PhoneNumberRules.Message);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد")
            .Matches("[A-Z]").WithMessage("رمز عبور باید حداقل یک حرف بزرگ داشته باشد")
            .Matches("[a-z]").WithMessage("رمز عبور باید حداقل یک حرف کوچک داشته باشد")
            .Matches("[0-9]").WithMessage("رمز عبور باید حداقل یک عدد داشته باشد");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("تکرار رمز عبور مطابقت ندارد");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("نقش نامعتبر است")
            .NotEqual(UserRole.Admin).WithMessage("امکان ثبت‌نام با نقش مدیر وجود ندارد");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است")
            .Matches(PhoneNumberRules.Pattern).WithMessage(PhoneNumberRules.Message);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است");
    }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("توکن بازنشانی الزامی است");
    }
}
