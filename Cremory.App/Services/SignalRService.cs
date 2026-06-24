using Microsoft.AspNetCore.SignalR.Client;
using Cremory.App.Models;

namespace Cremory.App.Services
{
    public class SignalRService : IDisposable
    {
        private HubConnection? _connection;
        private readonly string _hubUrl;
        private bool _disposed;
        private bool _started;

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

        public async Task EnsureStartedAsync()
        {
            if (_started) return;
            _started = true;

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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection?.DisposeAsync();
        }
    }
}
