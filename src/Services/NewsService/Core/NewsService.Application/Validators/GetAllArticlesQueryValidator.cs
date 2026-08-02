using FluentValidation;
using NewsService.Application.Features.Queries.Article.Request;

namespace NewsService.Application.Validators;

public class GetAllArticlesQueryValidator : AbstractValidator<GetAllArticlesQuery>
{
    public GetAllArticlesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya üzeri olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır.");
    }
}
