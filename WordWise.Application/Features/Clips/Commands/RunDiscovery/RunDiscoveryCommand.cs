using MediatR;
using WordWise.Application.Features.Clips.Models;
using WordWise.Application.Features.Clips.Services;

namespace WordWise.Application.Features.Clips.Commands.RunDiscovery
{
    /// <summary>Admin panelinden elle çalıştırma: günlük tıklama hakkına (<see cref="DiscoveryRunQuota"/>) tabidir.</summary>
    public record RunDiscoveryCommand() : IRequest<DiscoveryRunResult>;

    public class RunDiscoveryCommandHandler(ClipDiscoveryRunner _runner, DiscoveryRunQuota _quota)
        : IRequestHandler<RunDiscoveryCommand, DiscoveryRunResult>
    {
        public async Task<DiscoveryRunResult> Handle(RunDiscoveryCommand request, CancellationToken cancellationToken)
        {
            // Hak önce harcanır: çalıştırma YouTube'a gidip yarıda hata verse bile kota harcanmış olabilir.
            var log = await _quota.ConsumeAsync(DateTime.UtcNow, cancellationToken);
            var result = await _runner.RunOnceAsync(cancellationToken);
            await _quota.RecordResultAsync(log, result.VideosProcessed, result.ClipsCreated, result.ClipsPublished, result.Failures.Count, cancellationToken);
            return result;
        }
    }

    public record GetDiscoveryRunStatusQuery() : IRequest<DiscoveryRunStatus>;

    public class GetDiscoveryRunStatusQueryHandler(DiscoveryRunQuota _quota)
        : IRequestHandler<GetDiscoveryRunStatusQuery, DiscoveryRunStatus>
    {
        public Task<DiscoveryRunStatus> Handle(GetDiscoveryRunStatusQuery request, CancellationToken cancellationToken) =>
            _quota.GetStatusAsync(DateTime.UtcNow, cancellationToken);
    }
}
