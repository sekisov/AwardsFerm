using AwardsFerm.Core.Interfaces;
using AwardsFerm.Infrastructure;
using AwardsFerm.Worker.Services;

var profilesRoot = FindProfilesRoot();
Directory.CreateDirectory(profilesRoot);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAwardsFermInfrastructure(profilesRoot);
builder.Services.AddSingleton<SessionExecutionService>();
builder.Services.AddSingleton<ISessionEventReporter, HttpSessionEventReporter>();

builder.Services.AddHttpClient("api", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

app.MapPost("/internal/run", (WorkerRunRequest request, SessionExecutionService executor) =>
{
    if (executor.IsProfileRunning(request.ProfileId))
        return Results.Conflict($"Профиль {request.ProfileId} уже выполняется.");

    executor.StartAsync(request);
    return Results.Accepted();
});

app.MapPost("/internal/stop/{profileId}", (string profileId, SessionExecutionService executor) =>
{
    executor.Stop(profileId);
    return Results.NoContent();
});

app.MapPost("/internal/stop", (SessionExecutionService executor) =>
{
    foreach (var profileId in new[] { "session-001", "session-002", "session-003" })
        executor.Stop(profileId);

    return Results.NoContent();
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static string FindProfilesRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null)
    {
        var candidate = Path.Combine(dir.FullName, "profiles");
        if (Directory.Exists(candidate))
            return candidate;
        dir = dir.Parent;
    }

    return Path.Combine(Directory.GetCurrentDirectory(), "profiles");
}
