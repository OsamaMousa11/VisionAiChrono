using Hangfire.Dashboard;

namespace VisionAiChrono.Api.Filters
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var connection = context.GetHttpContext().Connection;

            if (connection.RemoteIpAddress == null)
            {
                return true;
            }

            return System.Net.IPAddress.IsLoopback(connection.RemoteIpAddress);
        }
    }
}
