using FluentValidation;
using NewsService.Application.Features.Commands.Category.Request;

namespace NewsService.Application.Validators;

public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Kategori ID'si gereklidir.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı gereklidir.")
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
