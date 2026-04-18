using MediatR;

namespace NewsService.Application.Features.Commands.Category.Request;

public class DeleteCategoryCommand : IRequest
{
    public Guid Id { get; set; }
}
