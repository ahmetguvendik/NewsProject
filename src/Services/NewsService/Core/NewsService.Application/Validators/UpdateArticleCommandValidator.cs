using FluentValidation;
using NewsService.Application.Features.Commands.Article.Request;

namespace NewsService.Application.Validators;

public class UpdateArticleCommandValidator : AbstractValidator<UpdateArticleCommand>
{
    public UpdateArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Haber ID'si gereklidir.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Başlık gereklidir.")
            .MaximumLength(300).WithMessage("Başlık en fazla 300 karakter olabilir.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("İçerik gereklidir.");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Özet en fazla 500 karakter olabilir.");

        RuleFor(x => x.ImageUrl)
            .ValidImageReference();

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori seçilmelidir.");
    }
}
