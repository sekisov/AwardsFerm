using System.Text.Json;
using AwardsFerm.Core.Models;
using AwardsFerm.Core.Interfaces;

namespace AwardsFerm.Infrastructure.Storage;

public sealed class ProfileRepository : IProfileRepository
{
    public string ProfilesRoot { get; }

    public ProfileRepository(string profilesRoot)
    {
        ProfilesRoot = profilesRoot;
    }

    public async Task<DesktopProfile> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync("session-001", cancellationToken)
               ?? CreateDefaultProfile();
    }

    public async Task<DesktopProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(ProfilesRoot, id, "config.json");
        if (!File.Exists(path))
            return id == "session-001" ? CreateDefaultProfile() : null;

        await using var stream = File.OpenRead(path);
        var profile = await JsonSerializer.DeserializeAsync<DesktopProfile>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);
        if (profile is not null)
            profile.CookiesPath = Path.Combine(ProfilesRoot, id, "cookies.json");
        return profile;
    }

    private static DesktopProfile CreateDefaultProfile() => new()
    {
        Id = "session-001",
        Name = "Desktop Chrome Win10 — Санкт-Петербург",
        Latitude = 60.053085,
        Longitude = 30.311729,
        CookiesPath = Path.Combine("profiles", "session-001", "cookies.json")
    };
}
