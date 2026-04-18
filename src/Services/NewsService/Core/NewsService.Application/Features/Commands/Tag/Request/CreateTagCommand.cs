using MediatR;
using NewsService.Application.Features.Commands.Tag.Response;

namespace NewsService.Application.Features.Commands.Tag.Request;

public class CreateTagCommand : IRequest<CreateTagResponse>
{
    public string Name { get; set; } = string.Empty;
}
