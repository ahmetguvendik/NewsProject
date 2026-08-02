using FluentValidation;
using IdentityService.Application.Features.Commands.User.Request;

namespace IdentityService.Application.Validators;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Kullanıcı ID'si gereklidir.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir.")
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir.")
            .MaximumLength(100);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048);
    }
}   
