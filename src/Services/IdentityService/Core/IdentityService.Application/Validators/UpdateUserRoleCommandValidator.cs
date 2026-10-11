using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Domain.Constants;

namespace IdentityService.Application.Validators;

public class UpdateUserRoleCommandValidator : AbstractValidator<UpdateUserRoleCommand>
{
    public UpdateUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.RoleName)
            .Must(role => role is not null && RoleConstants.TryGetId(role) is not null)
            .WithMessage("Yetki user, editor ya da admin olmalıdır.");
    }
}
