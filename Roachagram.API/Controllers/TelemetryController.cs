using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Roachagram.API.Models;

namespace Roachagram.API.Controllers
{
    /// <summary>
    /// API controller that accepts telemetry payloads from remote clients and forwards them to Application Insights.
    /// Uses the injected <see cref="TelemetryClient"/> to emit events, traces, and exceptions.
    /// </summary>
    /// <param name="telemetry">The <see cref="TelemetryClient"/> instance provided by dependency injection.</param>
    [ApiController]
    [Route("api/[controller]")]
    public class TelemetryController(TelemetryClient telemetry) : ControllerBase
    {
        /// <summary>
        /// Backing telemetry client instance provided by the primary constructor.
        /// </summary>
        private readonly TelemetryClient _telemetry = telemetry;

        /// <summary>
        /// Accepts a telemetry payload and forwards it to Application Insights.
        /// </summary>
        /// <param name="dto">
        /// The telemetry payload received from the client.
        /// The <see cref="TelemetryDTO.Type"/> field determines how the payload is recorded:
        /// - "event": records an event via <see cref="TelemetryClient.TrackEvent(string, IDictionary{string, string})"/>.
        /// - "trace": records a trace via <see cref="TelemetryClient.TrackTrace(string, IDictionary{string, string})"/>.
        /// - "exception": records an exception via <see cref="TelemetryClient.TrackException(Exception, IDictionary{string, string})"/>.
        /// </param>
        /// <returns>
        /// Returns <see cref="IActionResult"/>:
        /// - <see cref="BadRequestResult"/> when the payload is null or the <see cref="TelemetryDTO.Type"/> is unknown.
        /// - <see cref="CreatedResult"/> when telemetry was accepted and forwarded.
        /// </returns>
        /// <remarks>
        /// This endpoint is intentionally permissive for development. In production, decorate with __Authorize__
        /// and validate tokens/keys to prevent abuse. The method augments any supplied <see cref="TelemetryDTO.Properties"/>
        /// with details from <see cref="TelemetryDTO.SerializedException"/> before forwarding.
        /// </remarks>
        [HttpPost]
        // Add [Authorize] and validate tokens/keys in production
        public IActionResult Post(TelemetryDTO dto)
        {
            if (dto == null) return BadRequest();

            var properties = dto.Properties ?? new Dictionary<string, string>();

            // Take the SerializedException and convert it to properties for a concatenated message



            switch (dto.Type?.ToLowerInvariant())
            {
                case "trace":
                    _telemetry.TrackTrace((dto.Name != null && dto.Message != null) ? $"{dto.Name} - {dto.Message}" : "No trace message provided", properties);
                    break;
                case "exception":
                    //add the exception properties
                    properties["ExceptionType"] = dto.SerializedException?.Type ?? "null";
                    properties["ExceptionMessage"] = dto.SerializedException?.Message ?? "null";
                    properties["ExceptionSource"] = dto.SerializedException?.Source ?? "null";
                    properties["ExceptionTargetSite"] = dto.SerializedException?.TargetSite ?? "null";
                    properties["ExceptionStackTrace"] = dto.SerializedException?.StackTrace ?? "null";
                    _telemetry.TrackException(new Exception($"Remote Exception {dto.SerializedException?.Message ?? "null"}"), properties);
                    break;
                default:
                    return BadRequest("unknown telemetry type. need trace or exception");
            }

            return Created();
        }
    }
}