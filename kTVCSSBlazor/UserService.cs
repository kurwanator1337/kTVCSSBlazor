using kTVCSSBlazor.Components.Pages.Players;
using Microsoft.JSInterop;

public class UserService
{
    [JSInvokable]
    public static Task NotifyUserDisconnected(string steam)
    {
        var users = kTVCSSBlazor.Hubs.kTVCSSHub.OnlineUsers.Where(x => x.Value.SteamId == steam);

        if (users.Any())
        {
            foreach (var user in users)
            {
                kTVCSSBlazor.Hubs.kTVCSSHub.OnlineUsers.TryRemove(user.Key, out _);
            }
        }

        users = kTVCSSBlazor.Hubs.kTVCSSHub.SearchUsers.Where(x => x.Value.SteamId == steam);

        if (users.Any())
        {
            foreach (var user in users)
            {
                kTVCSSBlazor.Hubs.kTVCSSHub.SearchUsers.TryRemove(user.Key, out _);
            }
        }

        return Task.CompletedTask;
    }
}