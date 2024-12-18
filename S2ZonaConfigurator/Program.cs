﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using S2ZonaConfigurator;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces.Services;
using S2ZonaConfigurator.Models;
using S2ZonaConfigurator.Services.ModService;


var host = StartupServices.CreateHostBuilder(args).Build();

try
{
    // Get services
    var serviceProvider = host.Services;
    var appConfig = serviceProvider.GetRequiredService<IOptions<AppConfig>>().Value;
    var pakManager = serviceProvider.GetRequiredService<IPakManager>();
    var configParser = serviceProvider.GetRequiredService<IConfigParser>();
    var modProcessor = serviceProvider.GetRequiredService<IModProcessor>();

    // Initialize services
    pakManager.Initialize();

    // Parse mod files
    var modDataMap = modProcessor.ParseModFiles(appConfig.Paths.ModsDirectory);
    // Extract a list of required config file paths
    var requiredConfigs = ModProcessor.GetRequiredConfigFiles(modDataMap);

    // Extract required config files from PAKs
    foreach (var config in requiredConfigs)
    {
        await pakManager.ExtractConfigFile(config);
    }

    // Copy extracted files to Mods directory. We will modify these files
    pakManager.CopyExtractedFilesToMods();

    // Process mods
    foreach (var (modFile, modData) in modDataMap)
    {
        modProcessor.ProcessMod(modFile, modData);
    }

    modProcessor.PrintFinalSummary();

    // Create PAK file with mods
    await pakManager.CreateModPak();

    // Generate Optional changelog
    if (appConfig.Options.OutputChangelogFile)
        modProcessor.GenerateChangelog(pakManager.GetOutputPakPath(), modDataMap);
}
catch (Exception ex)
{
    Printer.PrintExceptionMessage(ex);
}
finally
{
    // Stop the host gracefully
    await host.StopAsync();
    Console.ReadKey();
}