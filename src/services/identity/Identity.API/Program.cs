﻿namespace Identity.API
{
    using Identity.DataStores;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    using Npgsql;

    using Optional;

    using Polly;
    using Polly.Retry;

    using Serilog;

    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    /// <summary>
    /// Entry point of the application
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method of the program
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static async Task Main(string[] args)
        {
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            IHost host = CreateHostBuilder(args).Build();

            using IServiceScope scope = host.Services.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();
            IdentityContext context = services.GetRequiredService<IdentityContext>();
            IHostEnvironment hostingEnvironment = services.GetRequiredService<IHostEnvironment>();
            logger?.LogInformation("Starting {ApplicationContext}", hostingEnvironment.ApplicationName);

            try
            {
                if (!context.Database.IsInMemory())
                {
                    logger?.LogInformation("Upgrading {ApplicationContext}'s store", hostingEnvironment.ApplicationName);
                    // Forces database migrations on startup
                    RetryPolicy policy = Policy
                        .Handle<NpgsqlException>(sql => sql.Message.Like("*failed*", ignoreCase: true))
                        .WaitAndRetryAsync(
                            retryCount: 5,
                            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                            onRetry: (exception, timeSpan, attempt, pollyContext) =>

                                logger?.LogError(exception, "Error while upgrading database schema (Attempt {Attempt})", attempt)
                            );
                    logger?.LogInformation("Starting {ApplicationContext} database migration", hostingEnvironment.ApplicationName);

                    // Forces datastore migration on startup
                    await policy.ExecuteAsync(async () => await context.Database.MigrateAsync().ConfigureAwait(false))
                                .ConfigureAwait(false);

                    logger?.LogInformation($"Identity database updated");
                }
                await host.RunAsync()
                    .ConfigureAwait(false);

                logger?.LogInformation($"Identity.API started");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "An error occurred on startup.");
            }
        }


        /// <summary>
        /// Builds the host
        /// </summary>
        /// <param name="args">command line arguments</param>
        public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
                           .ConfigureWebHostDefaults(webHost => webHost.UseStartup<Startup>()
                                                                       .UseKestrel((hosting, options) => options.AddServerHeader = hosting.HostingEnvironment.IsDevelopment())
                                                                       .UseSerilog((hosting, loggerConfig) =>
                                                                       {
                                                                           loggerConfig = loggerConfig
                                                                              .MinimumLevel.Verbose()
                                                                              .Enrich.WithProperty("ApplicationContext", hosting.HostingEnvironment.ApplicationName)
                                                                              .Enrich.FromLogContext()
                                                                              .WriteTo.Console()
                                                                              .ReadFrom.Configuration(hosting.Configuration);

                                                                           hosting.Configuration.GetServiceUri("seq")
                                                                                                .SomeNotNull()
                                                                                                .MatchSome(seqUri => loggerConfig.WriteTo.Seq(seqUri.AbsoluteUri));
                                                                       })
                           )
                           .ConfigureLogging((options) =>
                           {
                               options.ClearProviders() // removes all default providers
                                   .AddSerilog()
                                   .AddConsole();
                           });
    }
}
