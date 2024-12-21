using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using S2ZonaConfigurator.Services.ModService;
using S2ZonaConfigurator.Services.PakService;
using System.Diagnostics.CodeAnalysis;


namespace S2ZonaConfigurator;
public static class StartupServices
{
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure<TOptions>(IConfiguration)")]
    private static IServiceCollection AddAppConfiguration(
        this IServiceCollection services,
        string[] args)
    {
        services.AddOptions();

        var env = Environment.GetEnvironmentVariable("S2_ZONA_CONFIGURATOR_ENVIRONMENT") ?? "Mine";

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env}.json", true, true)
            .AddEnvironmentVariables()
            .AddUserSecrets(typeof(Program).Assembly, optional: true)
            .AddCommandLine(args)
            .Build();

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConfiguration(configuration.GetSection("Logging"));
            logging.AddConsole();
        });

        services.Configure<AppConfig>(configuration.GetSection("AppConfig"));
        services.Configure<PathsConfig>(configuration.GetSection("AppConfig:Paths"));
        services.Configure<GameConfig>(configuration.GetSection("AppConfig:Game"));

        // Register IConfiguration for DI
        services.AddSingleton(configuration);

        return services;
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddAppConfiguration(args);

            services.AddTransient<IPakManager, PakManager>();
            services.AddTransient<IConfigParser, ConfigParser>();
            services.AddTransient<IModProcessor, ModProcessor>();
        });
}