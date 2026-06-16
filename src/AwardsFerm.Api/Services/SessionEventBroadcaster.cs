using AwardsFerm.Core.Interfaces;
using AwardsFerm.Core.Models;
using AwardsFerm.Api.Hubs;
using AwardsFerm.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace AwardsFerm.Api.Services;

public sealed class SessionEventBroadcaster : ISessionEventReporter
{
    private readonly IHubContext<SessionHub> _hub;
    private readonly SessionManager _sessionManager;

    public SessionEventBroadcaster(IHubContext<SessionHub> hub, SessionManager sessionManager)
    {
        _hub = hub;
        _sessionManager = sessionManager;
    }

    public async Task ReportAsync(SessionEvent sessionEvent, CancellationToken cancellationToken = default)
    {
        _sessionManager.ApplyEvent(sessionEvent);
        await _hub.Clients.All.SendAsync("SessionEvent", sessionEvent, cancellationToken);
    }
}
