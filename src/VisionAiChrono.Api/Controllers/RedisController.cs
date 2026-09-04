using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;
using System.Text.Json;

namespace VisionAiChrono.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisController : ControllerBase
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisController> _logger;

    public RedisController(IDistributedCache cache, ILogger<RedisController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Demonstrates cache behavior - first call takes time, subsequent calls are instant
    /// </summary>
    [HttpGet("demo")]
    public async Task<IActionResult> CacheDemo()
    {
        const string cacheKey = "demo_data";
        var stopwatch = Stopwatch.StartNew();

        // Try to get from cache
        var cachedValue = await _cache.GetStringAsync(cacheKey);

        if (cachedValue != null)
        {
            stopwatch.Stop();
            _logger.LogInformation("✅ Cache HIT - Retrieved from Redis in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                source = "Cache (Redis)",
                data = cachedValue,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }

        // Simulate expensive operation (database query, API call, etc.)
        await Task.Delay(2000); // 2 second delay to simulate work

        var newValue = $"Generated at {DateTime.UtcNow:O} - User: Osama";

        // Store in cache for 5 minutes
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            SlidingExpiration = TimeSpan.FromMinutes(1)
        };

        await _cache.SetStringAsync(cacheKey, newValue, cacheOptions);

        stopwatch.Stop();
        _logger.LogInformation("⚠️ Cache MISS - Data generated and cached in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            source = "Fresh Data (First Load)",
            data = newValue,
            executionTimeMs = stopwatch.ElapsedMilliseconds,
            timestamp = DateTime.UtcNow,
            message = "Data cached for 5 minutes. Try calling this endpoint again!"
        });
    }

    /// <summary>
    /// Get user data with caching
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserWithCache(string userId)
    {
        const string cacheKeyPrefix = "user_";
        var cacheKey = $"{cacheKeyPrefix}{userId}";
        var stopwatch = Stopwatch.StartNew();

        // Try cache first
        var cachedUser = await _cache.GetStringAsync(cacheKey);

        if (cachedUser != null)
        {
            stopwatch.Stop();
            _logger.LogInformation("✅ User cache HIT for userId: {UserId} in {ElapsedMilliseconds}ms", userId, stopwatch.ElapsedMilliseconds);

            return Ok(new
            {
                source = "Cache",
                data = JsonSerializer.Deserialize<object>(cachedUser),
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                cacheKey = cacheKey
            });
        }

        // Simulate database query
        await Task.Delay(1500);

        var userData = new
        {
            id = userId,
            name = $"User {userId}",
            email = $"user{userId}@example.com",
            retrievedAt = DateTime.UtcNow
        };

        var userJson = JsonSerializer.Serialize(userData);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        await _cache.SetStringAsync(cacheKey, userJson, cacheOptions);

        stopwatch.Stop();
        _logger.LogInformation("⚠️ User cache MISS for userId: {UserId} - Data fetched in {ElapsedMilliseconds}ms", userId, stopwatch.ElapsedMilliseconds);

        return Ok(new
        {
            source = "Database",
            data = userData,
            executionTimeMs = stopwatch.ElapsedMilliseconds,
            cacheKey = cacheKey,
            message = $"User data cached for 10 minutes"
        });
    }

    /// <summary>
    /// Clear specific cache entry
    /// </summary>
    [HttpDelete("cache/{key}")]
    public async Task<IActionResult> ClearCache(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogInformation("🗑️ Cache cleared for key: {CacheKey}", key);

            return Ok(new { message = $"Cache cleared for key: {key}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache for key: {CacheKey}", key);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test Redis connection and get stats
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> HealthCheck()
    {
        try
        {
            var testKey = "redis_health_check";
            var testValue = $"OK - {DateTime.UtcNow:O}";

            await _cache.SetStringAsync(testKey, testValue, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
            });

            var retrieved = await _cache.GetStringAsync(testKey);
            await _cache.RemoveAsync(testKey);

            if (retrieved == testValue)
            {
                _logger.LogInformation("✅ Redis health check passed");
                return Ok(new
                {
                    status = "Healthy",
                    message = "Redis connection is working correctly",
                    timestamp = DateTime.UtcNow
                });
            }

            return BadRequest(new { status = "Unhealthy", message = "Redis cache test failed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis health check failed");
            return StatusCode(500, new
            {
                status = "Error",
                message = "Redis connection failed",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Set a custom cache value
    /// </summary>
    [HttpPost("set")]
    public async Task<IActionResult> SetCache([FromQuery] string key, [FromQuery] string value, [FromQuery] int minutesToExpire = 5)
    {
        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutesToExpire)
            };

            await _cache.SetStringAsync(key, value, cacheOptions);

            _logger.LogInformation("✅ Cache set - Key: {CacheKey}, Expires in: {Minutes} minutes", key, minutesToExpire);

            return Ok(new
            {
                message = "Value cached successfully",
                key = key,
                value = value,
                expiresIn = $"{minutesToExpire} minutes"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a cache value
    /// </summary>
    [HttpGet("get")]
    public async Task<IActionResult> GetCache([FromQuery] string key)
    {
        try
        {
            var value = await _cache.GetStringAsync(key);

            if (value == null)
            {
                _logger.LogWarning("Cache key not found: {CacheKey}", key);
                return NotFound(new { message = $"Cache key '{key}' not found or expired" });
            }

            _logger.LogInformation("✅ Cache retrieved - Key: {CacheKey}", key);

            return Ok(new { key = key, value = value });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}