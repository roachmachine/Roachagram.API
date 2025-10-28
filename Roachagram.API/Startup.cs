using System;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Roachagram.API.Extensions;
using Roachagram.API.Models;

namespace Roachagram.API
{
    /// <summary>
    /// Represents the startup class for configuring the application.
    /// </summary>
    /// <author>Michael</author>
    /// <datetime>5/25/2017 7:03 PM</datetime>
    /// <remarks>Handles application initialization and configuration.</remarks>
    /// <remarks>
    /// Initializes a new instance of the <see cref="Startup"/> class.
    /// </remarks>
    /// <param name="configuration">The configuration settings for the application.</param>
    public class Startup(IConfiguration configuration)
    {
        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        /// <value>
        /// Provides access to configuration settings.
        /// </value>
        public IConfiguration Configuration { get; } = configuration;

        /// <summary>
        /// Configures services for the application.
        /// Adds retry logic for the database connection in case the database is unresponsive.
        /// Registers Application Insights telemetry.
        /// </summary>
        /// <param name="services">The service collection to which services are added.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds support for controllers in the application.
            services.AddControllers();

            // Adds memory caching services.
            services.AddMemoryCache();

            // Register Application Insights using configuration or environment variable.
            var aiConnectionString = Configuration["roach-machine-app-insights"]
                                     ?? Configuration["APPINSIGHTS_CONNECTIONSTRING"];
            if (!string.IsNullOrWhiteSpace(aiConnectionString))
            {
                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.ConnectionString = aiConnectionString;
                });
            }
        }

        /// <summary>
        /// Configures the HTTP request pipeline for the application.
        /// </summary>
        /// <param name="app">The application builder used to configure the request pipeline.</param>
        /// <param name="env">The hosting environment information.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Configure the application for development or production environments.
            if (env.IsDevelopment())
            {
                // Enables the developer exception page for detailed error information in development.
                app.UseDeveloperExceptionPage();
            }
            else if (env.IsProduction())
            {
                // Configures retry options for accessing Azure Key Vault in production.
                SecretClientOptions options = new()
                {
                    Retry =
                        {
                            Delay = TimeSpan.FromSeconds(30),
                            MaxDelay = TimeSpan.FromSeconds(240),
                            MaxRetries = 5,
                            Mode = RetryMode.Fixed
                        }
                };
            }

            // Enforces HTTPS redirection for all requests.
            app.UseHttpsRedirection();

            // Adds routing middleware to the request pipeline.
            app.UseRouting();

            // Adds authorization middleware to the request pipeline.
            app.UseAuthorization();

            app.UseRateLimiting(TimeSpan.FromSeconds(5));

            // Configures the endpoints for the application.
            app.UseEndpoints(endpoints =>
            {
                // Maps controller actions to endpoints.
                endpoints.MapControllers();
            });
        }
    }
}