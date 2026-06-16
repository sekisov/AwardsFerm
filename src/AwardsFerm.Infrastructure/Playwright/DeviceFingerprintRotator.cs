using AwardsFerm.Core.Models;

namespace AwardsFerm.Infrastructure.Playwright;

internal static class DeviceFingerprintRotator
{
    private static readonly Random Random = new();

    private static readonly (string Vendor, string Renderer)[] GpuProfiles =
    [
        ("Intel Inc.", "Intel Iris OpenGL Engine"),
        ("Intel Inc.", "Intel(R) UHD Graphics 630"),
        ("NVIDIA Corporation", "NVIDIA GeForce GTX 1660 SUPER/PCIe/SSE2"),
        ("NVIDIA Corporation", "NVIDIA GeForce RTX 3060/PCIe/SSE2"),
        ("AMD", "AMD Radeon RX 580 Series"),
        ("Google Inc. (Intel)", "ANGLE (Intel, Intel(R) UHD Graphics 630 Direct3D11 vs_5_0 ps_5_0, D3D11)")
    ];

    /// <summary>Случайный отпечаток устройства на каждый запуск сессии (базовый профиль не перезаписывается).</summary>
    public static DesktopProfile RotateForSession(DesktopProfile baseProfile, string profilesRoot)
    {
        var chromeMajor = Random.Next(120, 132);
        var chromeBuild = Random.Next(6100, 6800);
        var gpu = GpuProfiles[Random.Next(GpuProfiles.Length)];
        var cores = new[] { 4, 6, 8, 12, 16 }[Random.Next(5)];
        var memory = new[] { 4, 8, 16 }[Random.Next(3)];
        var proxyEntry = ProxyRotator.PickForProfile(profilesRoot, baseProfile.Id);
        string? proxy = proxyEntry?.Url;
        var geo = proxy is not null
            ? ProxyRotator.ResolveGeo(proxy, proxyEntry?.Geo).WithJitter(Random)
            : RussiaGeo.PickForProfile(baseProfile.Id, baseProfile).WithJitter(Random);

        var browserSessionId = Guid.NewGuid().ToString("N");
        var profileDir = Path.Combine(profilesRoot, baseProfile.Id);

        return new DesktopProfile
        {
            Id = baseProfile.Id,
            Name = baseProfile.Name,
            BrowserSessionId = browserSessionId,
            UserAgent =
                $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{chromeMajor}.0.{chromeBuild}.0 Safari/537.36",
            ViewportWidth = baseProfile.ViewportWidth > 0 ? baseProfile.ViewportWidth : 1920,
            ViewportHeight = baseProfile.ViewportHeight > 0 ? baseProfile.ViewportHeight : 1080,
            Locale = geo.Locale,
            Timezone = geo.Timezone,
            Latitude = geo.Latitude,
            Longitude = geo.Longitude,
            LocationLabel = geo.Label,
            ProxyUrl = null,
            CookiesPath = Path.Combine(profileDir, $"cookies-{browserSessionId}.json"),
            HardwareConcurrency = cores,
            DeviceMemory = memory,
            WebGlVendor = gpu.Vendor,
            WebGlRenderer = gpu.Renderer,
            Platform = "Win32",
            DeviceScaleFactor = 1,
            SessionDeviceId = Guid.NewGuid().ToString("N"),
            SessionMac = GenerateMacAddress()
        };
    }

    public static void PruneOldBrowserInstances(string profilesRoot, string profileId, int keep = 3)
    {
        var baseDir = Path.Combine(profilesRoot, profileId, "browser-data");
        if (!Directory.Exists(baseDir))
            return;

        foreach (var dir in Directory.GetDirectories(baseDir)
                     .OrderByDescending(Directory.GetCreationTimeUtc)
                     .Skip(keep))
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch
            {
                // ignore locked dirs
            }
        }

        var profileDir = Path.Combine(profilesRoot, profileId);
        foreach (var cookieFile in Directory.GetFiles(profileDir, "cookies-*.json")
                     .OrderByDescending(File.GetCreationTimeUtc)
                     .Skip(keep + 2))
        {
            try
            {
                File.Delete(cookieFile);
            }
            catch
            {
                // ignore
            }
        }
    }

    public static string Describe(DesktopProfile profile, string? publicIp = null)
    {
        var ipPart = publicIp is not null
            ? $"IP: {publicIp}"
            : profile.ProxyUrl is not null
                ? $"прокси: {MaskProxy(profile.ProxyUrl)} (IP уточняется после старта)"
                : "IP: ваш реальный (для смены — proxies.txt)";

        return $"ID: {profile.SessionDeviceId[..8]}…, MAC: {profile.SessionMac} (локальный, сайты не видят), {ipPart}, " +
               $"локация: {SessionLocationHelper.Format(profile)}, " +
               $"{profile.ViewportWidth}×{profile.ViewportHeight}, {profile.HardwareConcurrency} CPU, {profile.DeviceMemory} GB RAM";
    }

    private static string GenerateMacAddress()
    {
        var bytes = new byte[6];
        Random.NextBytes(bytes);
        bytes[0] = (byte)((bytes[0] & 0xFE) | 0x02);
        return string.Join(':', bytes.Select(b => b.ToString("X2")));
    }

    private static string MaskProxy(string proxyUrl)
    {
        try
        {
            var uri = new Uri(proxyUrl);
            if (string.IsNullOrEmpty(uri.UserInfo))
                return $"{uri.Scheme}://{uri.Host}:{uri.Port}";

            return $"{uri.Scheme}://***@{uri.Host}:{uri.Port}";
        }
        catch
        {
            return "настроен";
        }
    }
}
