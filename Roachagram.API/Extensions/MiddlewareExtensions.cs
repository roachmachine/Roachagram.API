using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Roachagram.API.Middleware;

namespace Roachagram.API.Extensions
{
    /// <summary>
    /// Extension methods for middleware registration.
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds the RateLimitingMiddleware to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The application builder.</param>
        /// <param name="slidingExpiration">The sliding expiration time for rate limiting.</param>
        /// <returns>The application builder.</returns>
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder, TimeSpan slidingExpiration)
        {
            var cache = builder.ApplicationServices.GetRequiredService<IMemoryCache>();
            return builder.UseMiddleware<RateLimitingMiddleware>(cache, slidingExpiration);
        }
    }
}
