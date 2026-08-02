using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.KeycloakId)
            .NotEmpty().WithMessage("KeycloakId gereklidir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta gereklidir.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(256);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.")
            .MaximumLength(100);
    }
}
