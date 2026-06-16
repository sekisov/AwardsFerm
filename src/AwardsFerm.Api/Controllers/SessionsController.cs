using AwardsFerm.Core.Models;
using AwardsFerm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AwardsFerm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SessionsController : ControllerBase
{
    private readonly SessionManager _sessionManager;
    private readonly SessionRunnerService _runner;

    public SessionsController(SessionManager sessionManager, SessionRunnerService runner)
    {
        _sessionManager = sessionManager;
        _runner = runner;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<SessionInfo>> GetAll()
    {
        return Ok(_sessionManager.GetAll());
    }

    [HttpGet("current")]
    public ActionResult<SessionInfo?> GetCurrent()
    {
        return Ok(_sessionManager.GetCurrent());
    }

    [HttpGet("{sessionId}")]
    public ActionResult<SessionInfo> GetById(string sessionId)
    {
        var session = _sessionManager.GetById(sessionId);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("start")]
    public async Task<ActionResult<SessionInfo>> Start(
        [FromBody] StartSessionRequest? request,
        CancellationToken cancellationToken)
    {
        request ??= new StartSessionRequest();
        request.Options ??= new YandexGamesSearchOptions { Headless = false };

        try
        {
            var session = await _runner.StartAsync(request, cancellationToken);
            return Ok(session);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{sessionId}/stop")]
    public async Task<IActionResult> StopById(string sessionId, CancellationToken cancellationToken)
    {
        var session = _sessionManager.GetById(sessionId);
        if (session is null)
            return NotFound();

        await _runner.StopProfileAsync(session.ProfileId, cancellationToken);
        return NoContent();
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopAll(CancellationToken cancellationToken)
    {
        foreach (var session in _sessionManager.GetAll()
                     .Where(s => s.Status is SessionStatus.Starting or SessionStatus.Running))
        {
            await _runner.StopProfileAsync(session.ProfileId, cancellationToken);
        }

        return NoContent();
    }
}
