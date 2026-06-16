using AwardsFerm.Core.Models;

namespace AwardsFerm.Core.Interfaces;

public interface ISessionEventReporter
{
    Task ReportAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default);
}
