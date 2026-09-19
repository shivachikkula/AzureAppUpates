using AzureEnvManager.Api.Models.Domain;

namespace AzureEnvManager.Api.Services;

public interface ILogStreamBroker
{
    /// <summary>Starts (or joins) the live Kudu log stream for an app and relays lines to the app's SignalR group.</summary>
    Task SubscribeAsync(AzureApplication app, string connectionId);

    Task UnsubscribeAsync(Guid applicationId, string connectionId);

    /// <summary>Called when a hub connection drops, to release every subscription it held.</summary>
    Task RemoveConnectionAsync(string connectionId);
}
