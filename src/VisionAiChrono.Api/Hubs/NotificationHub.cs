using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace VisionAiChrono.Api.Hubs
{
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = 
            new ConcurrentDictionary<string, UserConnection>();

        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
            var userConnection = new UserConnection
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow
            };

            _connections.TryAdd(Context.ConnectionId, userConnection);

            _logger.LogInformation("User connected - ConnectionId: {ConnectionId}, UserId: {UserId}", 
                Context.ConnectionId, userId);

            await Clients.All.SendAsync("UserConnected", new
            {
                userId,
                message = $"User {userId} connected",
                connectedAt = DateTime.UtcNow,
                totalConnections = _connections.Count
            });

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _connections.TryRemove(Context.ConnectionId, out var userConnection);

            _logger.LogInformation("User disconnected - ConnectionId: {ConnectionId}, UserId: {UserId}", 
                Context.ConnectionId, userConnection?.UserId ?? "Unknown");

            await Clients.All.SendAsync("UserDisconnected", new
            {
                userId = userConnection?.UserId,
                message = $"User disconnected",
                disconnectedAt = DateTime.UtcNow,
                totalConnections = _connections.Count
            });

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Send notification to all connected clients
        /// </summary>
        public async Task SendNotification(string title, string message)
        {
            _logger.LogInformation("Broadcasting notification: {Title} - {Message}", title, message);

            await Clients.All.SendAsync("ReceiveNotification", new
            {
                title,
                message,
                timestamp = DateTime.UtcNow,
                type = "info"
            });
        }

        /// <summary>
        /// Send notification to specific user
        /// </summary>
        public async Task SendPrivateNotification(string userId, string title, string message)
        {
            var userConnection = _connections.Values.FirstOrDefault(x => x.UserId == userId);

            if (userConnection != null)
            {
                _logger.LogInformation("Sending private notification to {UserId}", userId);

                await Clients.Client(userConnection.ConnectionId).SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    timestamp = DateTime.UtcNow,
                    type = "private"
                });
            }
        }

        /// <summary>
        /// Simulate real-time data updates (e.g., for dashboard)
        /// </summary>
        public async Task StartLiveDataStream()
        {
            _logger.LogInformation("Live data stream started for {ConnectionId}", Context.ConnectionId);

            await Clients.Caller.SendAsync("LiveDataStarted", "Real-time data stream started");

            // Simulate data updates every 2 seconds
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(2000);

                await Clients.Caller.SendAsync("LiveDataUpdate", new
                {
                    id = i + 1,
                    value = new Random().Next(100, 1000),
                    timestamp = DateTime.UtcNow,
                    label = $"Data Point {i + 1}"
                });
            }

            await Clients.Caller.SendAsync("LiveDataCompleted", "Real-time data stream completed");
        }

        /// <summary>
        /// Get current connection stats
        /// </summary>
        public async Task GetConnectionStats()
        {
            await Clients.Caller.SendAsync("ConnectionStats", new
            {
                totalConnections = _connections.Count,
                connections = _connections.Values.Select(x => new
                {
                    x.UserId,
                    x.ConnectionId,
                    x.ConnectedAt,
                    connectedFor = DateTime.UtcNow - x.ConnectedAt
                })
            });
        }
    }

    public class UserConnection
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}