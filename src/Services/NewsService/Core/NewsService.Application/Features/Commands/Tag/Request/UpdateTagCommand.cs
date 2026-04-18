using MediatR;
using NewsService.Application.Features.Commands.Tag.Response;

namespace NewsService.Application.Features.Commands.Tag.Request;

public class UpdateTagCommand : IRequest<UpdateTagResponse>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
