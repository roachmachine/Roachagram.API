using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Roachagram.API
{
    /// <summary>
    /// Main Program entry method
    /// </summary>
    /// <author>Michael</author>
    /// <datetime>5/25/2017 7:02 PM</datetime>
    /// <remarks>Main Program entry method</remarks>
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var builtConfig = config.Build();
                    var keyVaultEndpoint = builtConfig["KeyVault:Url"];
                    if (!string.IsNullOrEmpty(keyVaultEndpoint))
                    {
                        config.AddAzureKeyVault(new Uri(keyVaultEndpoint), new DefaultAzureCredential());
                    }
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}