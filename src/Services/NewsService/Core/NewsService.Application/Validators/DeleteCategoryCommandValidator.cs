using FluentValidation;
using NewsService.Application.Features.Commands.Category.Request;

namespace NewsService.Application.Validators;

public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Kategori ID'si gereklidir.");
    }
}
