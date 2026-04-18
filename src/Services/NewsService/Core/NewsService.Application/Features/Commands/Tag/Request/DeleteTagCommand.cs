using MediatR;

namespace NewsService.Application.Features.Commands.Tag.Request;

public class DeleteTagCommand : IRequest
{
    public Guid Id { get; set; }
}
