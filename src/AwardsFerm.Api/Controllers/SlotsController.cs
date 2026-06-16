using AwardsFerm.Api.Services;
using AwardsFerm.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace AwardsFerm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SlotsController : ControllerBase
{
    private readonly SessionSlotStore _slotStore;
    private readonly SessionRunnerService _runner;

    public SlotsController(SessionSlotStore slotStore, SessionRunnerService runner)
    {
        _slotStore = slotStore;
        _runner = runner;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<SessionSlotDefinition>> GetAll()
    {
        return Ok(_slotStore.GetAll());
    }

    [HttpPost]
    public ActionResult<SessionSlotDefinition> Create([FromBody] CreateSessionSlotRequest? request)
    {
        try
        {
            return Ok(_slotStore.Add(request?.Label));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPatch("{profileId}")]
    public ActionResult<SessionSlotDefinition> Update(
        string profileId,
        [FromBody] UpdateSessionSlotRequest request)
    {
        try
        {
            return Ok(_slotStore.Update(profileId, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{profileId}")]
    public async Task<IActionResult> Delete(string profileId, CancellationToken cancellationToken)
    {
        try
        {
            await _runner.StopProfileAsync(profileId, cancellationToken);
            _slotStore.Remove(profileId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
