using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
{
    public AssignRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Rol adı gereklidir.");
    }
}
