using MediatR;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.Application.Features.Clips.Commands.RunDiscovery
{
    public record RunDiscoveryCommand() : IRequest<DiscoveryRunResult>;

    public class RunDiscoveryCommandHandler(ClipDiscoveryRunner _runner)
        : IRequestHandler<RunDiscoveryCommand, DiscoveryRunResult>
    {
        public Task<DiscoveryRunResult> Handle(RunDiscoveryCommand request, CancellationToken cancellationToken) =>
            _runner.RunOnceAsync(cancellationToken);
    }
}
