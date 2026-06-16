namespace AwardsFerm.Core.Models;

public sealed class YandexGamesSearchOptions
{
    public string SearchQuery { get; set; } = "червячки";
    public string TargetGameTitle { get; set; } = "Slither Worms Wars!";
    public string TargetGameUrlPart { get; set; } = "slither-worms-wars-511328";
    public string TargetGameUrl => $"https://yandex.ru/games/app/{TargetGameUrlPart}";
    public int PlayDurationMinSeconds { get; set; } = 120;
    public int PlayDurationMaxSeconds { get; set; } = 180;
    public bool Headless { get; set; }
}
