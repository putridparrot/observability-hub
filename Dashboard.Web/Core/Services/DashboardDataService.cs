using System.Collections.Concurrent;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Plugins;

namespace Dashboard.Web.Core.Services;

public sealed class DashboardDataService
{
    private readonly PluginInstancesStore store;
    private readonly UserSettingsService userSettings;
    private readonly ConcurrentDictionary<string, IReadOnlyList<DashboardWidget>> moduleWidgets = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> nextRunUtc = new();
    private readonly SemaphoreSlim initLock = new(1, 1);

    public DashboardDataService(PluginInstancesStore store, UserSettingsService userSettings)
    {
        this.store = store;
        this.userSettings = userSettings;
        store.Changed += OnStoreChanged;
    }

    private void OnStoreChanged()
    {
        // Remove cached data for modules that no longer exist.
        var currentIds = store.GetAllModules().Select(m => m.Id).ToHashSet();
        foreach (var key in moduleWidgets.Keys.Where(k => !currentIds.Contains(k)).ToList())
        {
            moduleWidgets.TryRemove(key, out _);
            nextRunUtc.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Collects data for any module not yet initialized. Safe to call multiple times;
    /// new modules added after startup are picked up on the next call.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await initLock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var module in store.GetAllModules())
            {
                if (!moduleWidgets.ContainsKey(module.Id))
                {
                    await RunModuleAsync(module, now, cancellationToken);
                }
            }
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <summary>Returns widgets from modules flagged ShowOnDashboard, filtered by per-item user settings.</summary>
    public IReadOnlyList<DashboardWidget> GetDashboardWidgets()
    {
        var moduleMap = store.GetAllModules().ToDictionary(m => m.Id);
        return moduleWidgets
            .SelectMany(kvp => kvp.Value)
            .Where(w =>
            {
                if (!moduleMap.TryGetValue(w.ModuleId, out var m)) return false;
                return userSettings.IsOnDashboard(w.ModuleId, w.ItemKey, m.ShowOnDashboard);
            })
            .OrderBy(x => x.ModuleName)
            .ThenByDescending(x => x.Status)
            .ThenBy(x => x.Title)
            .ToList();
    }

    /// <summary>Returns widgets for all modules whose effective page group matches groupName.</summary>
    public IReadOnlyList<DashboardWidget> GetWidgetsForGroup(string groupName)
    {
        var groupIds = store.GetAllModules()
            .Where(m => string.Equals(m.GetEffectivePageGroup(), groupName, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToHashSet();

        return moduleWidgets
            .Where(kvp => groupIds.Contains(kvp.Key))
            .SelectMany(x => x.Value)
            .OrderBy(x => x.ModuleName)
            .ThenByDescending(x => x.Status)
            .ThenBy(x => x.Title)
            .ToList();
    }

    /// <summary>Returns the distinct effective page group names for all registered modules.</summary>
    public IReadOnlyList<string> GetPageGroups() =>
        store.GetAllModules()
            .Select(m => m.GetEffectivePageGroup())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();

    /// <summary>Re-collects a single module on demand, regardless of its RefreshMode or schedule.</summary>
    public async Task TriggerRefreshAsync(string moduleId, CancellationToken cancellationToken)
    {
        var module = store.GetAllModules().FirstOrDefault(m => m.Id == moduleId);
        if (module is null) return;

        await RunModuleAsync(module, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <summary>Polls every Auto-mode module that is past its scheduled next-run time,
    /// honouring per-module group refresh toggles from user settings.
    /// Also initializes any newly added modules.</summary>
    public async Task PollDueModulesAsync(CancellationToken cancellationToken)
    {
        // Pick up any modules added since last initialization.
        await InitializeAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var module in store.GetAllModules())
        {
            if (module.RefreshMode != RefreshMode.Auto) continue;
            var effective = userSettings.GetModuleEffectiveRefreshMode(module.Id, module.ItemKeys, module.RefreshMode);
            if (effective != RefreshMode.Auto) continue;

            var dueAt = nextRunUtc.GetOrAdd(module.Id, now);
            if (dueAt <= now)
            {
                await RunModuleAsync(module, now, cancellationToken);
            }
        }
    }

    private async Task RunModuleAsync(IDashboardModule module, DateTimeOffset now, CancellationToken cancellationToken)
    {
        try
        {
            var widgets = await module.CollectAsync(cancellationToken);
            moduleWidgets[module.Id] = widgets;
        }
        catch (Exception ex)
        {
            moduleWidgets[module.Id] =
            [
                new DashboardWidget(
                    module.Id,
                    module.DisplayName,
                    "Module execution error",
                    ex.Message,
                    WidgetStatus.Critical,
                    DateTimeOffset.UtcNow)
            ];
        }

        var refresh = module.RefreshInterval <= TimeSpan.Zero ? TimeSpan.FromMinutes(1) : module.RefreshInterval;
        nextRunUtc[module.Id] = now + refresh;
    }
}
