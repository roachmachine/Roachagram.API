using System;
using AnagramAPI;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        // Stores the database connection string.
        private string _connection = null;

        /// <summary>
        /// Gets the application configuration.
        /// </summary>
        /// <value>
        /// Provides access to configuration settings.
        /// </value>
        public IConfiguration Configuration { get; } = configuration;

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <param name="services">The service collection to which services are added.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Adds support for controllers in the application.
            services.AddControllers();

            // Adds memory caching services.
            services.AddMemoryCache();

            // Configures the application's database context with the SQL Server connection string.
            services.AddDbContext<DictionaryDBContext>((options) =>
            {
                options.UseSqlServer(_connection);
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline for the application.
        /// </summary>
        /// <param name="app">The application builder used to configure the request pipeline.</param>
        /// <param name="env">The hosting environment information.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Retrieve the connection string directly from the configuration.
            var connectionString = Configuration["roachagram-db-connection-string"];

            // Build the SQL connection string using the retrieved connection string.
            var builder = new SqlConnectionStringBuilder(connectionString);

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
                            Delay = TimeSpan.FromSeconds(2),
                            MaxDelay = TimeSpan.FromSeconds(16),
                            MaxRetries = 5,
                            Mode = RetryMode.Exponential
                        }
                };
            }

            // Assign the built connection string to the private field.
            _connection = builder.ConnectionString;

            // Enforces HTTPS redirection for all requests.
            app.UseHttpsRedirection();

            // Adds routing middleware to the request pipeline.
            app.UseRouting();

            // Adds authorization middleware to the request pipeline.
            app.UseAuthorization();

            // Configures the endpoints for the application.
            app.UseEndpoints(endpoints =>
            {
                // Maps controller actions to endpoints.
                endpoints.MapControllers();
            });
        }
    }
}