namespace AwardsFerm.Api.Options;

public sealed class YandexRsyaOptions
{
    public const string SectionName = "YandexRsya";

    public string OAuthToken { get; set; } = string.Empty;
    public string Currency { get; set; } = "RUB";
    public int RefreshSeconds { get; set; } = 120;
    public string ApiBaseUrl { get; set; } = "https://partner.yandex.ru/api/statistics2/get.json";
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";
    public string JwtIssuer { get; set; } = "AwardsFerm";
    public string JwtAudience { get; set; } = "AwardsFermClient";
    public string JwtSecret { get; set; } = "CHANGE_ME_MIN_32_CHARS_SECRET_12345";
    public int JwtExpiresHours { get; set; } = 24;
}
