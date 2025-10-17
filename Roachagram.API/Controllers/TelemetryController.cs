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

            var properties = ConvertToStringDictionary(dto.Properties);

            //take the SerializedException and convert to properties to a nice concatenated message
            properties.Add("ExceptionType", dto.SerializedException?.Type ?? "null");
            properties.Add("ExceptionMessage", dto.SerializedException?.Message ?? "null");
            properties.Add("ExceptionSource", dto.SerializedException?.Source ?? "null");
            properties.Add("ExceptionTargetSite", dto.SerializedException?.TargetSite ?? "null");
            properties.Add("ExceptionStackTrace", dto.SerializedException?.StackTrace ?? "null");

            switch (dto.Type?.ToLowerInvariant())
            {
                case "event":
                    _telemetry.TrackEvent(dto.Type ?? "unnamed_event", properties);
                    break;
                case "trace":
                    _telemetry.TrackTrace(dto.Type ?? "unnamed_event", properties);
                    break;
                case "exception":

                    _telemetry.TrackException(new Exception($"Remote Exception {dto.SerializedException?.Message ?? "null"}"), properties);
                    break;
                default:
                    return BadRequest("unknown telemetry type. need event, trace, or exception");
            }

            return Created();
        }

        /// <summary>
        /// Converts a loosely-typed object containing key/value pairs into a string dictionary suitable for telemetry properties.
        /// </summary>
        /// <param name="properties">
        /// The source properties to convert. Supported shapes:
        /// - <see cref="IDictionary{TKey, TValue}"/> with <c>string, string</c> entries.
        /// - non-generic <see cref="IDictionary"/> implementations such as <see cref="Hashtable"/>.
        /// - A POCO whose public instance readable properties will be reflected and converted to strings.
        /// </param>
        /// <returns>
        /// A case-insensitive <see cref="Dictionary{TKey, TValue}"/> containing stringified property values,
        /// or <c>null</c> when <paramref name="properties"/> is <c>null</c> or contains no entries.
        /// </returns>
        /// <remarks>
        /// - When given an <see cref="IDictionary{string, string}"/>, the method returns a copy to prevent callers from
        ///   mutating the original dictionary.
        /// - For non-generic dictionaries, keys and values are stringified using <see cref="object.ToString"/>.
        /// - For reflected POCOs, any property getter that throws will be ignored.
        /// </remarks>
        private static Dictionary<string, string> ConvertToStringDictionary(object properties)
        {
            if (properties == null) return null;

            // If already the expected type
            if (properties is IDictionary<string, string> typedDict)
            {
                return typedDict.Count > 0 ? new Dictionary<string, string>(typedDict, StringComparer.OrdinalIgnoreCase) : null;
            }

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Handle non-generic IDictionary (e.g., Hashtable)
            if (properties is IDictionary nonGenericDict)
            {
                foreach (DictionaryEntry entry in nonGenericDict)
                {
                    var key = entry.Key?.ToString();
                    if (string.IsNullOrEmpty(key)) continue;
                    var value = entry.Value?.ToString();
                    if (value == null) continue;
                    result[key] = value;
                }

                return result.Count > 0 ? result : null;
            }

            // Reflect over public instance properties
            var props = properties.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.CanRead);

            foreach (var prop in props)
            {
                var key = prop.Name;
                if (string.IsNullOrEmpty(key)) continue;
                object valueObj;
                try
                {
                    valueObj = prop.GetValue(properties);
                }
                catch
                {
                    // Ignore properties that throw on get
                    continue;
                }

                if (valueObj == null) continue;
                result[key] = valueObj.ToString();
            }

            return result.Count > 0 ? result : null;
        }
    }
}