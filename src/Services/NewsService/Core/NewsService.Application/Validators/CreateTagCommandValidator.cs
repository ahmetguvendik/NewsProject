using FluentValidation;
using NewsService.Application.Features.Commands.Tag.Request;

namespace NewsService.Application.Validators;

public class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Etiket adı gereklidir.")
            .MaximumLength(50);
    }
}
