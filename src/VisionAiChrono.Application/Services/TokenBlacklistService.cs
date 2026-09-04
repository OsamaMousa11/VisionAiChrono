using Microsoft.Extensions.Caching.Memory;
using CleanArchitectureTemplate_Application.ServiceContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitectureTemplate_Application.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _cache;

        public TokenBlacklistService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public void BlacklistToken(string token, DateTime expiry)
        {
            var timeToLive = expiry - DateTime.UtcNow;
            if (timeToLive <= TimeSpan.Zero)
                return;

            _cache.Set($"bl_{token}", true, timeToLive);
        }

        public bool IsBlacklisted(string token)
        {
            return _cache.TryGetValue($"bl_{token}", out _);
        }
    }
}
