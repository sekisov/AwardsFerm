using AwardsFerm.Api.Options;
using AwardsFerm.Api.Services;
using AwardsFerm.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AwardsFerm.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RsyaController : ControllerBase
{
    private readonly YandexRsyaStatisticsService _statisticsService;
    private readonly YandexRsyaOptions _options;

    public RsyaController(YandexRsyaStatisticsService statisticsService, IOptions<YandexRsyaOptions> options)
    {
        _statisticsService = statisticsService;
        _options = options.Value;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<RsyaDashboard>> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _statisticsService.GetDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        return Ok(new
        {
            configured = _statisticsService.IsConfigured,
            refreshSeconds = _options.RefreshSeconds,
            currency = _options.Currency
        });
    }
}
