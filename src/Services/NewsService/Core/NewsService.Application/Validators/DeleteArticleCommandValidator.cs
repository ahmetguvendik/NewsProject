using FluentValidation;
using NewsService.Application.Features.Commands.Article.Request;

namespace NewsService.Application.Validators;

public class DeleteArticleCommandValidator : AbstractValidator<DeleteArticleCommand>
{
    public DeleteArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Makale ID'si gereklidir.");
    }
}
