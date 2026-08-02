using FluentValidation;
using IdentityService.Application.Features.Queries.User.Request;

namespace IdentityService.Application.Validators;

public class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    public GetAllUsersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya üzeri olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Sayfa boyutu 1-100 arasında olmalıdır.");
    }
}
