using System;

namespace Roachagram.API.Models
{
    /// <summary>
    /// Represents a serialized form of an <see cref="Exception"/> suitable for telemetry transport or storage.
    /// Contains the exception type name, message, stack trace and origin information.
    /// </summary>
    public class SerializedException
    {
        /// <summary>
        /// Gets or sets the CLR type name of the exception (for example, "System.NullReferenceException").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the exception message that describes the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the stack trace information captured from the exception.
        /// May be null or empty if not available.
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the name of the application or object that caused the exception.
        /// This maps to <see cref="Exception.Source"/>.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the method that threw the exception.
        /// This maps to <see cref="Exception.TargetSite"/> (method information serialized as a string).
        /// </summary>
        public string TargetSite { get; set; }
    }

    /// <summary>
    /// Contains additional contextual properties collected with telemetry events.
    /// </summary>
    /// <remarks>
    /// Instances of this class carry optional contextual information such as the UI page,
    /// handler name, application version, device model and operating system. Fields may be null
    /// or empty if the information is not available.
    /// </remarks>
    public class Properties
    {
        /// <summary>
        /// Gets or sets the logical page or route where the telemetry event originated (for example, "Home/Index").
        /// </summary>
        public string Page { get; set; }

        /// <summary>
        /// Gets or sets the handler or component name responsible for the action (for example, controller or service name).
        /// </summary>
        public string Handler { get; set; }

        /// <summary>
        /// Gets or sets the application version string (for example, "1.2.3" or a build identifier).
        /// </summary>
        public string AppVersion { get; set; }

        /// <summary>
        /// Gets or sets the device model identifier if telemetry is collected from a device (for example, "iPhone12,1" or "Surface Pro 7").
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// Gets or sets the operating system name and version for the environment that generated the telemetry (for example, "Windows 10.0.19044" or "iOS 16.0").
        /// </summary>
        public string OS { get; set; }
    }

    /// <summary>
    /// Top-level Data Transfer Object for telemetry payloads sent to the telemetry endpoint.
    /// </summary>
    public class TelemetryDTO
    {
        /// <summary>
        /// Gets or sets the telemetry event type or category (for example, "Error", "Metric", "Trace").
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the serialized exception data associated with this telemetry event, if any.
        /// </summary>
        public SerializedException SerializedException { get; set; }

        /// <summary>
        /// Gets or sets the additional contextual properties collected with this telemetry event.
        /// </summary>
        public Properties Properties { get; set; }
    }
}
