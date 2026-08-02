using FluentValidation;
using NewsService.Application.Features.Commands.Tag.Request;

namespace NewsService.Application.Validators;

public class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Etiket ID'si gereklidir.");
    }
}
