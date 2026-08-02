using FluentValidation;
using NewsService.Application.Features.Commands.Article.Request;

namespace NewsService.Application.Validators;

public class PublishArticleCommandValidator : AbstractValidator<PublishArticleCommand>
{
    public PublishArticleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Makale ID'si gereklidir.");
    }
}
