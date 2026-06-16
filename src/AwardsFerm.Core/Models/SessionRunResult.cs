namespace AwardsFerm.Core.Models;

public sealed class SessionRunResult
{
    public bool AutoRestartAfterGameOvers { get; init; }
    public bool BrowserClosedUnexpectedly { get; init; }
    public int GameOverCount { get; init; }

    public bool ShouldAutoRestart => AutoRestartAfterGameOvers || BrowserClosedUnexpectedly;

    public static SessionRunResult Completed(int gameOverCount = 0) => new()
    {
        AutoRestartAfterGameOvers = false,
        BrowserClosedUnexpectedly = false,
        GameOverCount = gameOverCount
    };

    public static SessionRunResult RestartAfterGameOvers(int gameOverCount) => new()
    {
        AutoRestartAfterGameOvers = true,
        BrowserClosedUnexpectedly = false,
        GameOverCount = gameOverCount
    };

    public static SessionRunResult BrowserClosed(int gameOverCount = 0) => new()
    {
        AutoRestartAfterGameOvers = false,
        BrowserClosedUnexpectedly = true,
        GameOverCount = gameOverCount
    };
}
