using Microsoft.Extensions.DependencyInjection;
using S2ZonaConfigurator;
using S2ZonaConfigurator.Helpers;
using S2ZonaConfigurator.Interfaces;

var host = StartupServices.CreateHostBuilder(args).Build();

try
{
    var serviceProvider = host.Services;

    var appService = serviceProvider.GetRequiredService<IAppService>();
    await appService.RunAsync();
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