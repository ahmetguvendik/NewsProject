using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class ActivateUserCommandValidator : AbstractValidator<ActivateUserCommand>
{
    public ActivateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");
    }
}
