using AwardsFerm.Core.Models;

namespace AwardsFerm.Api.Services;

public sealed class SessionRunnerService
{
    private readonly SessionManager _sessionManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SessionRunnerService> _logger;

    public SessionRunnerService(
        SessionManager sessionManager,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SessionRunnerService> logger)
    {
        _sessionManager = sessionManager;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SessionInfo> StartAsync(StartSessionRequest request, CancellationToken cancellationToken = default)
    {
        request.Options ??= new YandexGamesSearchOptions { Headless = false };

        var session = _sessionManager.StartSession(request);
        var workerUrl = _configuration["Worker:BaseUrl"] ?? "http://localhost:8081";
        var payload = new WorkerRunRequest
        {
            SessionId = session.Id,
            ProfileId = session.ProfileId,
            AutoRestart = session.AutoRestart,
            Options = request.Options
        };

        try
        {
            var client = _httpClientFactory.CreateClient("worker");
            var response = await client.PostAsJsonAsync($"{workerUrl}/internal/run", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _sessionManager.StopSession(session.Id);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(body) ? "Worker не смог запустить сессию." : body);
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _sessionManager.StopSession(session.Id);
            throw new InvalidOperationException($"Worker недоступен: {ex.Message}", ex);
        }

        _logger.LogInformation("Сессия {SessionId} запущена для {ProfileId}", session.Id, session.ProfileId);
        return session;
    }

    public async Task StopProfileAsync(string profileId, CancellationToken cancellationToken = default)
    {
        var session = _sessionManager.GetByProfileId(profileId);
        if (session is null)
            return;

        _sessionManager.StopSession(session.Id);

        var workerUrl = _configuration["Worker:BaseUrl"] ?? "http://localhost:8081";
        try
        {
            var client = _httpClientFactory.CreateClient("worker");
            await client.PostAsync($"{workerUrl}/internal/stop/{profileId}", null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось остановить Worker для {ProfileId}", profileId);
        }
    }

    private sealed class WorkerRunRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string ProfileId { get; set; } = "session-001";
        public bool AutoRestart { get; set; } = true;
        public YandexGamesSearchOptions Options { get; set; } = new();
    }
}
