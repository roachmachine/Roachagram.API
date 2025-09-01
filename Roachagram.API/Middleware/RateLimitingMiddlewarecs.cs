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
            // Retrieve the device ID from the request headers
            var deviceId = context.Request.Headers["X-Device-ID"].FirstOrDefault();

            // If the device ID is missing, return a 400 Bad Request response
            if (string.IsNullOrEmpty(deviceId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Missing X-Device-ID header.");
                return;
            }

            // Generate a unique cache key for the device ID
            var cacheKey = $"RateLimit-{deviceId}";

            // Check if the device ID is already in the cache
            if (!_cache.TryGetValue(cacheKey, out _))
            {
                // If not in the cache, add it with a sliding expiration
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    SlidingExpiration = _slidingExpiration
                };

                _cache.Set(cacheKey, true, cacheEntryOptions);
            }
            else
            {
                // If the device ID is in the cache, return a 429 Too Many Requests response
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            // Proceed to the next middleware in the pipeline
            await _next(context);
        }
    }
}
