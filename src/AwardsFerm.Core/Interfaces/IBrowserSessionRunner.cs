using AwardsFerm.Core.Models;

namespace AwardsFerm.Core.Interfaces;

public interface IBrowserSessionRunner
{
    Task<SessionRunResult> RunYandexGamesSearchAsync(
        string sessionId,
        DesktopProfile profile,
        YandexGamesSearchOptions options,
        CancellationToken cancellationToken = default);
}
