using System.Text.Json;
using System.Text.RegularExpressions;
using AwardsFerm.Core.Models;

namespace AwardsFerm.Api.Services;

public sealed class SessionSlotStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _slotsPath;
    private readonly string _profilesRoot;
    private readonly object _lock = new();

    public SessionSlotStore()
    {
        _profilesRoot = ProfilesPathHelper.FindProfilesRoot();
        Directory.CreateDirectory(_profilesRoot);
        _slotsPath = Path.Combine(_profilesRoot, "slots.json");
        EnsureDefaults();
    }

    public string ProfilesRoot => _profilesRoot;

    public IReadOnlyList<SessionSlotDefinition> GetAll()
    {
        lock (_lock)
        {
            return Load().Slots.Select(Clone).ToList();
        }
    }

    public int Count => GetAll().Count;

    public SessionSlotDefinition Add(string? label = null)
    {
        lock (_lock)
        {
            var config = Load();
            if (config.Slots.Count >= 10)
                throw new InvalidOperationException("Достигнут лимит слотов (10).");

            var nextIndex = config.Slots
                .Select(s => ParseSessionNumber(s.ProfileId))
                .DefaultIfEmpty(0)
                .Max() + 1;

            var profileId = $"session-{nextIndex:D3}";
            var slot = new SessionSlotDefinition
            {
                ProfileId = profileId,
                Label = string.IsNullOrWhiteSpace(label) ? $"Сессия {nextIndex}" : label.Trim(),
                ScheduleEnabled = false,
                ScheduledStartMsk = null
            };

            EnsureProfileDirectory(profileId, slot.Label, nextIndex - 1);
            config.Slots.Add(slot);
            Save(config);
            return Clone(slot);
        }
    }

    public SessionSlotDefinition Update(string profileId, UpdateSessionSlotRequest request)
    {
        lock (_lock)
        {
            var config = Load();
            var slot = config.Slots.FirstOrDefault(s => s.ProfileId == profileId)
                       ?? throw new InvalidOperationException($"Слот {profileId} не найден.");

            if (!string.IsNullOrWhiteSpace(request.Label))
                slot.Label = request.Label.Trim();

            if (request.ScheduleEnabled.HasValue)
                slot.ScheduleEnabled = request.ScheduleEnabled.Value;

            if (request.ScheduledStartMsk is not null)
            {
                var normalized = NormalizeMskTime(request.ScheduledStartMsk);
                slot.ScheduledStartMsk = normalized;
            }

            Save(config);
            return Clone(slot);
        }
    }

    public void Remove(string profileId)
    {
        lock (_lock)
        {
            var config = Load();
            if (config.Slots.Count <= 1)
                throw new InvalidOperationException("Нельзя удалить последний слот.");

            var removed = config.Slots.RemoveAll(s => s.ProfileId == profileId);
            if (removed == 0)
                throw new InvalidOperationException($"Слот {profileId} не найден.");

            Save(config);
        }
    }

    public bool Exists(string profileId) =>
        GetAll().Any(s => s.ProfileId == profileId);

    private void EnsureDefaults()
    {
        lock (_lock)
        {
            if (File.Exists(_slotsPath))
                return;

            var config = new SessionSlotsConfig
            {
                Slots =
                [
                    new SessionSlotDefinition
                    {
                        ProfileId = "session-001",
                        Label = "Сессия 1",
                        ScheduleEnabled = false
                    },
                    new SessionSlotDefinition
                    {
                        ProfileId = "session-002",
                        Label = "Сессия 2",
                        ScheduleEnabled = false
                    }
                ]
            };

            foreach (var (slot, index) in config.Slots.Select((s, i) => (s, i)))
                EnsureProfileDirectory(slot.ProfileId, slot.Label, index);

            Save(config);
        }
    }

    private SessionSlotsConfig Load()
    {
        if (!File.Exists(_slotsPath))
        {
            EnsureDefaults();
        }

        var json = File.ReadAllText(_slotsPath);
        var config = JsonSerializer.Deserialize<SessionSlotsConfig>(json, JsonOptions)
                     ?? new SessionSlotsConfig();

        if (config.Slots.Count == 0)
        {
            EnsureDefaults();
            json = File.ReadAllText(_slotsPath);
            config = JsonSerializer.Deserialize<SessionSlotsConfig>(json, JsonOptions)
                     ?? new SessionSlotsConfig();
        }

        return config;
    }

    private void Save(SessionSlotsConfig config)
    {
        File.WriteAllText(_slotsPath, JsonSerializer.Serialize(config, JsonOptions));
    }

    private void EnsureProfileDirectory(string profileId, string label, int cityIndex)
    {
        var profileDir = Path.Combine(_profilesRoot, profileId);
        Directory.CreateDirectory(profileDir);

        var configPath = Path.Combine(profileDir, "config.json");
        if (File.Exists(configPath))
            return;

        var (lat, lon, cityLabel) = cityIndex switch
        {
            0 => (60.053085, 30.311729, "Санкт-Петербург, Россия"),
            1 => (55.7558, 37.6173, "Москва, Россия"),
            _ => (55.7558, 37.6173, "Москва, Россия")
        };

        var profile = new
        {
            id = profileId,
            name = $"Desktop Chrome Win10 — {label}",
            userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            viewportWidth = 1920,
            viewportHeight = 1080,
            locale = "ru-RU",
            timezone = "Europe/Moscow",
            latitude = lat,
            longitude = lon,
            locationLabel = cityLabel,
            proxyUrl = (string?)null
        };

        File.WriteAllText(configPath, JsonSerializer.Serialize(profile, JsonOptions));
    }

    private static int ParseSessionNumber(string profileId)
    {
        var match = Regex.Match(profileId, @"session-(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 0;
    }

    private static string? NormalizeMskTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (!Regex.IsMatch(trimmed, @"^\d{1,2}:\d{2}$"))
            throw new InvalidOperationException("Время должно быть в формате HH:mm (МСК).");

        var parts = trimmed.Split(':');
        var hours = int.Parse(parts[0]);
        var minutes = int.Parse(parts[1]);
        if (hours is < 0 or > 23 || minutes is < 0 or > 59)
            throw new InvalidOperationException("Некорректное время (МСК).");

        return $"{hours:D2}:{minutes:D2}";
    }

    private static SessionSlotDefinition Clone(SessionSlotDefinition source) => new()
    {
        ProfileId = source.ProfileId,
        Label = source.Label,
        ScheduleEnabled = source.ScheduleEnabled,
        ScheduledStartMsk = source.ScheduledStartMsk
    };
}
