using MediatR;
using NewsService.Application.Features.Commands.Category.Response;

namespace NewsService.Application.Features.Commands.Category.Request;

public class UpdateCategoryCommand : IRequest<UpdateCategoryResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
