using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");
    }
}
