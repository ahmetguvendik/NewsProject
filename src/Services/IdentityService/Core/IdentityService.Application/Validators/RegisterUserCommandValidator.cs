using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parola gereklidir.")
            .MinimumLength(8).WithMessage("Parola en az 8 karakter olmalıdır.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.")
            .MaximumLength(100);
    }
}
