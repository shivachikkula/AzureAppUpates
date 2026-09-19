using AzureEnvManager.Api.Extensions;
using AzureEnvManager.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AzureEnvManager.Api.Hubs;

[Authorize]
public class LogStreamHub(IAppAccessService accessService, IAzureApplicationLookup applicationLookup, ILogStreamBroker broker)
    : Hub
{
    public async Task Subscribe(Guid applicationId)
    {
        var userObjectId = Context.User!.GetObjectId();

        if (!await accessService.HasAccessAsync(userObjectId, applicationId))
        {
            throw new HubException("You do not have access to that application.");
        }

        var app = await applicationLookup.GetAsync(applicationId);
        if (app is null)
        {
            throw new HubException("Application not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, applicationId.ToString());
        await broker.SubscribeAsync(app, Context.ConnectionId);
    }

    public async Task Unsubscribe(Guid applicationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, applicationId.ToString());
        await broker.UnsubscribeAsync(applicationId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await broker.RemoveConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
