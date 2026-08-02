using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");
    }
}
