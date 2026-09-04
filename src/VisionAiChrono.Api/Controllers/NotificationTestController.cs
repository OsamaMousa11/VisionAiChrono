using VisionAiChrono.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace VisionAiChrono.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationTestController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationTestController> _logger;

        public NotificationTestController(IHubContext<NotificationHub> hubContext, ILogger<NotificationTestController> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Broadcast notification to all connected clients
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<IActionResult> BroadcastNotification([FromQuery] string title, [FromQuery] string message)
        {
            try
            {
                _logger.LogInformation("Broadcasting notification: {Title}", title);

                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    timestamp = DateTime.UtcNow,
                    type = "broadcast"
                });

                return Ok(new { success = true, message = "Notification broadcasted" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting notification");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        /// <summary>
        /// Get connection statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ConnectionStats");
                return Ok(new { message = "Stats request sent to all clients" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Send test notification
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                {
                    title = "Test Notification",
                    message = $"This is a test sent at {DateTime.UtcNow:O}",
                    timestamp = DateTime.UtcNow,
                    type = "test"
                });

                return Ok(new { success = true, message = "Test notification sent" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test notification");
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}