using AwardsFerm.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace AwardsFerm.Api.Hubs;

public sealed class SessionHub : Hub
{
    public async Task JoinSession()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "sessions");
    }
}
