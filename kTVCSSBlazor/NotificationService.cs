using Microsoft.JSInterop;
using System.Threading.Tasks;

public class NotificationService
{
    private readonly IJSRuntime _jsRuntime;

    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task RequestPermissionAsync()
    {
        await _jsRuntime.InvokeVoidAsync("notifications.requestPermission");
    }

    public async Task ShowNotificationAsync(string title, string message)
    {
        var options = new
        {
            body = message
        };

        await _jsRuntime.InvokeVoidAsync("notifications.showNotification", title, options);
    }
}