namespace AwardsFerm.Infrastructure.Playwright;

internal sealed class ProxyGeoLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Timezone { get; init; } = "Europe/Moscow";
    public string Locale { get; init; } = "ru-RU";
    public string Label { get; init; } = string.Empty;

    public ProxyGeoLocation WithJitter(Random random, double latDelta = 0.018, double lonDelta = 0.025)
    {
        return new ProxyGeoLocation
        {
            Latitude = Latitude + (random.NextDouble() - 0.5) * latDelta,
            Longitude = Longitude + (random.NextDouble() - 0.5) * lonDelta,
            Timezone = Timezone,
            Locale = Locale,
            Label = Label
        };
    }
}

internal sealed class ProxyEntry
{
    public required string Url { get; init; }
    public ProxyGeoLocation? Geo { get; init; }
}

internal static class ProxyRotator
{
    private static readonly Random Random = new();
    private static readonly object Lock = new();
    private static ProxyEntry[] _cache = [];
    private static string? _cachePath;
    private static DateTime _cacheMtime = DateTime.MinValue;
    private static readonly Dictionary<string, int> RotationIndex = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, ProxyGeoLocation> KnownHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["120.26.123.95:8010"] = new() { Latitude = 30.25, Longitude = 120.17, Timezone = "Asia/Shanghai", Locale = "zh-CN", Label = "Ханчжоу, Китай" },
        ["203.146.80.98:8080"] = new() { Latitude = 13.75, Longitude = 100.50, Timezone = "Asia/Bangkok", Locale = "th-TH", Label = "Таиланд" },
        ["217.60.63.215:1080"] = new() { Latitude = 48.8566, Longitude = 2.3522, Timezone = "Europe/Paris", Locale = "fr-FR", Label = "Париж, Франция" },
        ["193.39.168.88:1080"] = new() { Latitude = 55.7558, Longitude = 37.6173, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Москва, Россия" },
        ["12.89.176.82:3128"] = new() { Latitude = 41.4993, Longitude = -81.6944, Timezone = "America/New_York", Locale = "en-US", Label = "Кливленд, США" },
        ["154.160.53.2:8888"] = new() { Latitude = 5.6037, Longitude = -0.1870, Timezone = "Africa/Accra", Locale = "en-GH", Label = "Аккра, Гана" },
        ["157.245.100.190:442"] = new() { Latitude = 12.9716, Longitude = 77.5946, Timezone = "Asia/Kolkata", Locale = "en-IN", Label = "Бангалор, Индия" },
        ["109.71.246.44:1080"] = new() { Latitude = 55.7558, Longitude = 37.6173, Timezone = "Europe/Moscow", Locale = "ru-RU", Label = "Россия" },
        ["186.96.16.117:1080"] = new() { Latitude = 40.4168, Longitude = -3.7038, Timezone = "Europe/Madrid", Locale = "es-ES", Label = "Испания" },
    };

    public static string? PickNext(string profilesRoot) => PickForProfile(profilesRoot, null)?.Url;

    public static ProxyEntry? PickForProfile(string profilesRoot, string? profileId)
    {
        var proxies = LoadProxies(profilesRoot);
        if (proxies.Length == 0)
            return null;

        if (string.IsNullOrWhiteSpace(profileId))
            return proxies[Random.Next(proxies.Length)];

        var index = GetAndIncrementRotationIndex(profileId);
        return proxies[index % proxies.Length];
    }

    public static ProxyGeoLocation ResolveGeo(string? proxyUrl, ProxyGeoLocation? inlineGeo = null)
    {
        if (inlineGeo is not null && !string.IsNullOrWhiteSpace(inlineGeo.Label))
            return inlineGeo;

        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            var hostKey = ExtractHostKey(proxyUrl);
            if (hostKey is not null && KnownHosts.TryGetValue(hostKey, out var known))
                return known;
        }

        return new ProxyGeoLocation
        {
            Latitude = 55.7558,
            Longitude = 37.6173,
            Timezone = "Europe/Moscow",
            Locale = "ru-RU",
            Label = "Россия"
        };
    }

    private static int GetAndIncrementRotationIndex(string profileId)
    {
        lock (Lock)
        {
            if (!RotationIndex.TryGetValue(profileId, out var index))
                index = profileId switch
                {
                    "session-001" => 0,
                    "session-002" => 1,
                    "session-003" => 2,
                    _ => Math.Abs(profileId.GetHashCode(StringComparison.Ordinal))
                };

            RotationIndex[profileId] = index + 1;
            return index;
        }
    }

    private static ProxyEntry[] LoadProxies(string profilesRoot)
    {
        var path = Path.Combine(profilesRoot, "proxies.txt");
        lock (Lock)
        {
            var mtime = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            if (_cachePath != path || mtime != _cacheMtime)
            {
                _cachePath = path;
                _cacheMtime = mtime;
                _cache = File.Exists(path)
                    ? File.ReadAllLines(path)
                        .Select(ParseLine)
                        .Where(e => e is not null)
                        .Cast<ProxyEntry>()
                        .ToArray()
                    : [];
            }
        }

        return _cache;
    }

    private static ProxyEntry? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            return null;

        var parts = trimmed.Split('|', StringSplitOptions.TrimEntries);
        var url = parts[0];
        if (string.IsNullOrWhiteSpace(url) || !url.Contains("://", StringComparison.Ordinal))
            return null;

        ProxyGeoLocation? geo = null;
        if (parts.Length >= 4
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat)
            && double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lon))
        {
            geo = new ProxyGeoLocation
            {
                Latitude = lat,
                Longitude = lon,
                Timezone = parts[3],
                Locale = parts.Length > 4 ? parts[4] : "ru-RU",
                Label = parts.Length > 5 ? parts[5] : string.Empty
            };
        }

        geo = ResolveGeo(url, geo);
        return new ProxyEntry { Url = url, Geo = geo };
    }

    private static string? ExtractHostKey(string proxyUrl)
    {
        try
        {
            var uri = new Uri(proxyUrl);
            return $"{uri.Host}:{uri.Port}";
        }
        catch
        {
            return null;
        }
    }
}
