using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Plugins;

namespace Dashboard.Web.Core.Services;

public sealed class ModulePollingService(
    DashboardDataService dashboardDataService,
    PluginInstancesStore store,
    ILogger<ModulePollingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await dashboardDataService.InitializeAsync(stoppingToken);

        // Use 5-second tick; PollDueModulesAsync handles per-module scheduling.
        // Keeps the loop responsive enough to pick up newly added plugin instances.
        var tickInterval = store.GetAllModules()
            .Where(m => m.RefreshMode == RefreshMode.Auto && m.RefreshInterval > TimeSpan.Zero)
            .Select(m => m.RefreshInterval)
            .DefaultIfEmpty(TimeSpan.FromSeconds(30))
            .Min();

        if (tickInterval < TimeSpan.FromSeconds(5))
        {
            tickInterval = TimeSpan.FromSeconds(5);
        }

        using var timer = new PeriodicTimer(tickInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await dashboardDataService.PollDueModulesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed while polling dashboard modules");
            }
        }
    }
}
