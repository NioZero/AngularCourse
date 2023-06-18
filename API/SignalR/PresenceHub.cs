using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR;

[Authorize]
public class PresenceHub : Hub
{
    private readonly PresenceTracker Tracker;

    public PresenceHub(PresenceTracker tracker)
    {
        Tracker = tracker;
    }

    public override async Task OnConnectedAsync()
    {
        var isOnline = await Tracker.UserConnected(Context.User.GetUsername(), Context.ConnectionId);

        if(isOnline) await Clients.Others.SendAsync("UserIsOnline", Context.User.GetUsername());

        var currentUsers = await Tracker.GetOnlineUsers();
        await Clients.Caller.SendAsync("GetOnlineUsers", currentUsers);
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var isOffline = await Tracker.UserDisconnected(Context.User.GetUsername(), Context.ConnectionId);

        if(isOffline) await Clients.Others.SendAsync("UserIsOffline", Context.User.GetUsername());
       
        await base.OnDisconnectedAsync(exception);
    }
}