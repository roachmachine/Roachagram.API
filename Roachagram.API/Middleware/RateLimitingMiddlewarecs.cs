using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Roachagram.API.Middleware
{
    // Middleware to enforce rate limiting based on a device ID provided in the request header
    public class RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache, TimeSpan slidingExpiration)
    {
        // The next middleware in the pipeline
        private readonly RequestDelegate _next = next;

        // In-memory cache to store rate-limiting information
        private readonly IMemoryCache _cache = cache;

        // Sliding expiration time for cache entries
        private readonly TimeSpan _slidingExpiration = slidingExpiration;

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip rate limiting for health check endpoints
            if (context.Request.Path.StartsWithSegments("/api/home", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Retrieve the device ID from the request headers
            var deviceId = context.Request.Headers["X-Device-ID"].FirstOrDefault();

            // If the device ID is missing, return a 400 Bad Request response
            if (string.IsNullOrEmpty(deviceId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing X-Device-ID header.");
                return;
            }

            // Build rate-limit keys.
            // NOTE: RemoteIpAddress may be a proxy IP unless Forwarded Headers are configured.
            var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var ipKey = $"RateLimitIp-{remoteIp}";
            var ipDeviceKey = $"RateLimitIpDevice-{remoteIp}-{deviceId}";

            // If either the IP or IP+Device limit is active, reject the request.
            if (_cache.TryGetValue(ipKey, out _) || _cache.TryGetValue(ipDeviceKey, out _))
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Otherwise, set both keys with the same sliding expiration.
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = _slidingExpiration
            };

            _cache.Set(ipKey, true, cacheEntryOptions);
            _cache.Set(ipDeviceKey, true, cacheEntryOptions);

            // Proceed to the next middleware in the pipeline
            await _next(context);
        }
    }
}
