using AwardsFerm.Core.Models;
using AwardsFerm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AwardsFerm.Api.Controllers;

[ApiController]
[Route("api/internal")]
public sealed class InternalEventsController : ControllerBase
{
    private readonly SessionEventBroadcaster _broadcaster;

    public InternalEventsController(SessionEventBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
    }

    [HttpPost("events")]
    public async Task<IActionResult> PostEvent([FromBody] SessionEvent sessionEvent, CancellationToken cancellationToken)
    {
        await _broadcaster.ReportAsync(sessionEvent, cancellationToken);
        return Accepted();
    }
}
