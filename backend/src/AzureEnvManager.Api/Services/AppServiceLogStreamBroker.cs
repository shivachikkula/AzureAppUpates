using System.Collections.Concurrent;
using System.Net.Http.Headers;
using AzureEnvManager.Api.Hubs;
using AzureEnvManager.Api.Models.Domain;
using AzureEnvManager.Api.Models.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace AzureEnvManager.Api.Services;

/// <summary>
/// Bridges each Azure App Service's Kudu "logstream" endpoint (https://{app}.scm.azurewebsites.net/api/logstream)
/// to the browser over SignalR. One background pump runs per application while at least one client is
/// subscribed; it stops itself once the last subscriber disconnects.
/// </summary>
public class AppServiceLogStreamBroker : ILogStreamBroker, IDisposable
{
    private sealed class StreamState
    {
        public required Guid ApplicationId { get; init; }
        public required AzureApplication Application { get; init; }
        public readonly HashSet<string> ConnectionIds = new();
        public CancellationTokenSource Cts = new();
        public Task? PumpTask;
    }

    private readonly ConcurrentDictionary<Guid, StreamState> _streams = new();
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _connectionSubscriptions = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly IHubContext<LogStreamHub> _hub;
    private readonly IAzureAppServiceClient _azureClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AppServiceLogStreamBroker> _logger;

    public AppServiceLogStreamBroker(
        IHubContext<LogStreamHub> hub,
        IAzureAppServiceClient azureClient,
        IHttpClientFactory httpClientFactory,
        ILogger<AppServiceLogStreamBroker> logger)
    {
        _hub = hub;
        _azureClient = azureClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SubscribeAsync(AzureApplication app, string connectionId)
    {
        await _gate.WaitAsync();
        try
        {
            var state = _streams.GetOrAdd(app.Id, _ => new StreamState { ApplicationId = app.Id, Application = app });
            state.ConnectionIds.Add(connectionId);

            _connectionSubscriptions.AddOrUpdate(
                connectionId,
                _ => new HashSet<Guid> { app.Id },
                (_, set) => { set.Add(app.Id); return set; });

            if (state.PumpTask is null)
            {
                state.PumpTask = Task.Run(() => PumpAsync(state, state.Cts.Token));
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UnsubscribeAsync(Guid applicationId, string connectionId)
    {
        await _gate.WaitAsync();
        try
        {
            RemoveSubscriptionLocked(applicationId, connectionId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        await _gate.WaitAsync();
        try
        {
            if (_connectionSubscriptions.TryRemove(connectionId, out var applicationIds))
            {
                foreach (var applicationId in applicationIds)
                {
                    RemoveSubscriptionLocked(applicationId, connectionId, alreadyRemovedFromIndex: true);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private void RemoveSubscriptionLocked(Guid applicationId, string connectionId, bool alreadyRemovedFromIndex = false)
    {
        if (!alreadyRemovedFromIndex && _connectionSubscriptions.TryGetValue(connectionId, out var set))
        {
            set.Remove(applicationId);
            if (set.Count == 0)
            {
                _connectionSubscriptions.TryRemove(connectionId, out _);
            }
        }

        if (!_streams.TryGetValue(applicationId, out var state))
        {
            return;
        }

        state.ConnectionIds.Remove(connectionId);
        if (state.ConnectionIds.Count == 0)
        {
            _streams.TryRemove(applicationId, out _);
            state.Cts.Cancel();
        }
    }

    private async Task PumpAsync(StreamState state, CancellationToken ct)
    {
        var app = state.Application;
        var groupName = app.Id.ToString();

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var (username, password, scmHost) = await _azureClient.GetPublishingCredentialsAsync(app, ct);

                using var client = _httpClientFactory.CreateClient(nameof(AppServiceLogStreamBroker));
                var basicAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                client.Timeout = Timeout.InfiniteTimeSpan;

                using var response = await client.GetAsync(
                    $"https://{scmHost}/api/logstream",
                    HttpCompletionOption.ResponseHeadersRead,
                    ct);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line is null)
                    {
                        break; // Kudu closed the stream; loop around and reconnect.
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var entry = LogEntryDto.Parse(line);
                    await _hub.Clients.Group(groupName).SendAsync("LogReceived", entry, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Log stream for {App} dropped; retrying shortly", app.Name);
                await _hub.Clients.Group(groupName).SendAsync(
                    "LogReceived",
                    new LogEntryDto(DateTime.UtcNow.ToString("O"), "Warning", "Log stream disconnected, reconnecting..."),
                    CancellationToken.None);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public void Dispose()
    {
        foreach (var state in _streams.Values)
        {
            state.Cts.Cancel();
        }
        _gate.Dispose();
    }
}
