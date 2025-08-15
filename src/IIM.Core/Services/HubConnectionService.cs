using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace IIM.Core.Services
{
    public interface IHubConnectionService
    {
        Task<HubConnection> ConnectAsync(string hubName, CancellationToken cancellationToken = default);
        Task DisconnectAsync();
        bool IsConnected { get; }
    }

    public class HubConnectionService : IHubConnectionService, IAsyncDisposable
    {
        private readonly ILogger<HubConnectionService> _logger;
        private HubConnection? _connection;
        private readonly string _baseUrl;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public HubConnectionService(ILogger<HubConnectionService> logger, string baseUrl = "http://localhost:5000")
        {
            _logger = logger;
            _baseUrl = baseUrl;
        }

        public async Task<HubConnection> ConnectAsync(string hubName, CancellationToken cancellationToken = default)
        {
            if (_connection != null && IsConnected)
            {
                return _connection;
            }

            var url = $"{_baseUrl}/hubs/{hubName}";

            _connection = new HubConnectionBuilder()
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            _connection.Reconnecting += (error) =>
            {
                _logger.LogWarning("Connection lost, attempting to reconnect: {Error}", error?.Message);
                return Task.CompletedTask;
            };

            _connection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation("Reconnected with connection ID: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            _connection.Closed += (error) =>
            {
                _logger.LogError("Connection closed: {Error}", error?.Message);
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync(cancellationToken);
                _logger.LogInformation("Connected to hub: {HubName}", hubName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to hub: {HubName}", hubName);
                throw;
            }

            return _connection;
        }

        public async Task DisconnectAsync()
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
                _logger.LogInformation("Disconnected from hub");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}