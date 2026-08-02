using FluentValidation;
using NewsService.Application.Features.Commands.Tag.Request;

namespace NewsService.Application.Validators;

public class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Etiket ID'si gereklidir.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etiket adı gereklidir.")
            .MaximumLength(50);
    }
}
