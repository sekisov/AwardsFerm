using System.Collections.Concurrent;
using AwardsFerm.Core.Interfaces;
using AwardsFerm.Core.Models;

namespace AwardsFerm.Worker.Services;

public sealed class SessionExecutionService
{
    private readonly IBrowserSessionRunner _runner;
    private readonly IProfileRepository _profileRepository;
    private readonly ILogger<SessionExecutionService> _logger;

    private readonly ConcurrentDictionary<string, ProfileExecution> _byProfile = new();

    public SessionExecutionService(
        IBrowserSessionRunner runner,
        IProfileRepository profileRepository,
        ILogger<SessionExecutionService> logger)
    {
        _runner = runner;
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public bool IsProfileRunning(string profileId) =>
        _byProfile.TryGetValue(profileId, out var entry) && !entry.Task.IsCompleted;

    public Task StartAsync(WorkerRunRequest request)
    {
        if (_byProfile.TryGetValue(request.ProfileId, out var existing) && !existing.Task.IsCompleted)
            throw new InvalidOperationException($"Профиль {request.ProfileId} уже выполняется.");

        var cts = new CancellationTokenSource();
        var execution = new ProfileExecution
        {
            SessionId = request.SessionId,
            ProfileId = request.ProfileId,
            AutoRestart = request.AutoRestart,
            Cts = cts
        };

        execution.Task = Task.Run(() => RunProfileLoopAsync(execution, request.Options), cts.Token);
        _byProfile[request.ProfileId] = execution;

        return Task.CompletedTask;
    }

    public void Stop(string profileId)
    {
        if (_byProfile.TryGetValue(profileId, out var execution))
            execution.Cts.Cancel();
    }

    private async Task RunProfileLoopAsync(ProfileExecution execution, YandexGamesSearchOptions options)
    {
        var restartDelay = TimeSpan.FromSeconds(5);

        try
        {
            while (!execution.Cts.Token.IsCancellationRequested)
            {
                try
                {
                    var profile = await _profileRepository.GetByIdAsync(execution.ProfileId, execution.Cts.Token)
                                    ?? await _profileRepository.GetDefaultAsync(execution.Cts.Token);

                    options.Headless = ResolveHeadless(options);

                    _logger.LogInformation(
                        "Profile {ProfileId}: запуск браузера (session {SessionId})",
                        execution.ProfileId,
                        execution.SessionId);

                    var result = await _runner.RunYandexGamesSearchAsync(
                        execution.SessionId,
                        profile,
                        options,
                        execution.Cts.Token);

                    if (result.AutoRestartAfterGameOvers)
                    {
                        _logger.LogInformation(
                            "Profile {ProfileId}: {Games} игр — новая сессия с другим отпечатком и профилем браузера",
                            execution.ProfileId,
                            result.GameOverCount);
                    }
                }
                catch (OperationCanceledException) when (execution.Cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Profile {ProfileId}: сбой — перезапуск браузера", execution.ProfileId);
                }

                if (execution.Cts.Token.IsCancellationRequested)
                    break;

                if (!execution.AutoRestart)
                    break;

                _logger.LogInformation(
                    "Profile {ProfileId}: перезапуск браузера через {Seconds} сек",
                    execution.ProfileId,
                    restartDelay.TotalSeconds);

                try
                {
                    await Task.Delay(restartDelay, execution.Cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        finally
        {
            _byProfile.TryRemove(execution.ProfileId, out _);
        }
    }

    private bool ResolveHeadless(YandexGamesSearchOptions options)
    {
        var envHeadless = Environment.GetEnvironmentVariable("BROWSER_HEADLESS");
        if (!string.IsNullOrEmpty(envHeadless) && bool.TryParse(envHeadless, out var fromEnv))
            return fromEnv;

        return options.Headless;
    }

    private sealed class ProfileExecution
    {
        public required string SessionId { get; init; }
        public required string ProfileId { get; init; }
        public required bool AutoRestart { get; init; }
        public required CancellationTokenSource Cts { get; init; }
        public Task Task { get; set; } = Task.CompletedTask;
    }
}

public sealed class WorkerRunRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string ProfileId { get; set; } = "session-001";
    public bool AutoRestart { get; set; } = true;
    public YandexGamesSearchOptions Options { get; set; } = new();
}
