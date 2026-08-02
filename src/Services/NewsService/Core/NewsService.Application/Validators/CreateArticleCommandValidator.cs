using FluentValidation;
using NewsService.Application.Features.Commands.Article.Request;

namespace NewsService.Application.Validators;

public class CreateArticleCommandValidator : AbstractValidator<CreateArticleCommand>
{
    public CreateArticleCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık gereklidir.")
            .MaximumLength(300);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik gereklidir.");

        RuleFor(x => x.Summary)
            .MaximumLength(500);

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmelidir.");
    }
}
