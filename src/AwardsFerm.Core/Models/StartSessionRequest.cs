namespace AwardsFerm.Core.Models;

public sealed class StartSessionRequest
{
    public long? AdAccountId { get; set; }
    public string? ProfileId { get; set; }
    /// <summary>Работать до времени МСК, формат HH:mm. После достижения — STOP.</summary>
    public string? StopAtMsk { get; set; }
    public bool? AutoRestart { get; set; }
    public YandexGamesSearchOptions? Options { get; set; }
}
