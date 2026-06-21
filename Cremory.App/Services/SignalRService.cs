using Microsoft.AspNetCore.SignalR.Client;
using Cremory.App.Models;

namespace Cremory.App.Services
{
    public class SignalRService : IDisposable
    {
        private HubConnection? _connection;
        private readonly string _hubUrl;
        private bool _disposed;

        public event Action<OrderDto>? OrderCreated;
        public event Action<OrderDto>? OrderUpdated;
        public event Action<string>? OrderDeleted;
        public event Action<bool>? ConnectionStateChanged;

        public bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        public SignalRService()
        {
            _hubUrl = AppConfig.SignalrHub;
        }

        public async Task StartAsync()
        {
            if (_connection != null)
            {
                if (_connection.State == HubConnectionState.Connected)
                    return;

                await _connection.DisposeAsync();
            }

            _connection = new HubConnectionBuilder()
                .WithUrl(_hubUrl)
                .WithAutomaticReconnect(new[]
                {
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(30)
                })
                .Build();

            _connection.On<OrderDto>("OrderCreated", order =>
            {
                MainThread.BeginInvokeOnMainThread(() => OrderCreated?.Invoke(order));
            });

            _connection.On<OrderDto>("OrderUpdated", order =>
            {
                MainThread.BeginInvokeOnMainThread(() => OrderUpdated?.Invoke(order));
            });

            _connection.On<string>("OrderDeleted", orderId =>
            {
                MainThread.BeginInvokeOnMainThread(() => OrderDeleted?.Invoke(orderId));
            });

            _connection.Reconnecting += _ =>
            {
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(false));
                return Task.CompletedTask;
            };

            _connection.Reconnected += _ =>
            {
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(true));
                return Task.CompletedTask;
            };

            _connection.Closed += _ =>
            {
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(false));
                return Task.CompletedTask;
            };

            try
            {
                await _connection.StartAsync();
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SignalR connection failed: {ex.Message}");
                MainThread.BeginInvokeOnMainThread(() => ConnectionStateChanged?.Invoke(false));
            }
        }

        public async Task StopAsync()
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                ConnectionStateChanged?.Invoke(false);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _ = StopAsync();
            _connection?.DisposeAsync();
        }
    }
}
