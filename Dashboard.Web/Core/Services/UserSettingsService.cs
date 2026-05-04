using System.Text.Json;
using Dashboard.Web.Core.Models;

namespace Dashboard.Web.Core.Services;

public sealed class UserSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string settingsPath;
    private DashboardUserSettings settings;
    private readonly SemaphoreSlim saveLock = new(1, 1);

    /// <summary>Raised whenever any setting changes, so subscribers can refresh state.</summary>
    public event Action? SettingsChanged;

    public UserSettingsService(IWebHostEnvironment env)
    {
        settingsPath = Path.Combine(env.ContentRootPath, "dashboardUserSettings.json");
        settings = Load();
    }

    // ── Group-level refresh toggle ─────────────────────────────────────────────

    /// <summary>True (default) means the module auto-refreshes in the background.</summary>
    public bool IsGroupRefreshEnabled(string moduleId) =>
        !settings.GroupRefreshEnabled.TryGetValue(moduleId, out var enabled) || enabled;

    public void SetGroupRefreshEnabled(string moduleId, bool enabled)
    {
        settings.GroupRefreshEnabled[moduleId] = enabled;
        _ = SaveAsync();
        SettingsChanged?.Invoke();
    }

    // ── Per-item settings ──────────────────────────────────────────────────────

    private static string CompositeKey(string moduleId, string itemKey) => $"{moduleId}::{itemKey}";

    public ItemSettings GetItemSettings(string moduleId, string itemKey) =>
        settings.Items.TryGetValue(CompositeKey(moduleId, itemKey), out var s) ? s : new ItemSettings();

    public void SetItemSettings(string moduleId, string itemKey, ItemSettings itemSettings)
    {
        settings.Items[CompositeKey(moduleId, itemKey)] = itemSettings;
        _ = SaveAsync();
        SettingsChanged?.Invoke();
    }

    // ── Effective values (used by services) ────────────────────────────────────

    /// <summary>
    /// Whether a specific item should appear on the main dashboard.
    /// Falls back to <paramref name="moduleDefault"/> when no override is stored.
    /// </summary>
    public bool IsOnDashboard(string moduleId, string itemKey, bool moduleDefault) =>
        settings.Items.TryGetValue(CompositeKey(moduleId, itemKey), out var s)
            ? s.ShowOnDashboard
            : moduleDefault;

    /// <summary>
    /// Effective RefreshMode for one item, accounting for the group toggle and any per-item override.
    /// </summary>
    public RefreshMode GetEffectiveItemRefreshMode(string moduleId, string itemKey, RefreshMode moduleDefault)
    {
        if (!IsGroupRefreshEnabled(moduleId))
        {
            return RefreshMode.Manual;
        }

        if (!settings.Items.TryGetValue(CompositeKey(moduleId, itemKey), out var s))
        {
            return moduleDefault;
        }

        return s.RefreshMode.Equals("Auto", StringComparison.OrdinalIgnoreCase) ? RefreshMode.Auto
             : s.RefreshMode.Equals("Manual", StringComparison.OrdinalIgnoreCase) ? RefreshMode.Manual
             : moduleDefault;
    }

    /// <summary>
    /// Effective refresh interval in seconds for one item.
    /// Falls back to <paramref name="moduleDefaultSeconds"/> when no override is stored.
    /// </summary>
    public int GetEffectiveItemRefreshInterval(string moduleId, string itemKey, int moduleDefaultSeconds) =>
        settings.Items.TryGetValue(CompositeKey(moduleId, itemKey), out var s) && s.RefreshIntervalSeconds.HasValue
            ? s.RefreshIntervalSeconds.Value
            : moduleDefaultSeconds;

    /// <summary>
    /// Whether the module as a whole should auto-refresh:
    /// false if the group toggle is off, or if every item is explicitly set to Manual.
    /// </summary>
    public RefreshMode GetModuleEffectiveRefreshMode(
        string moduleId,
        IReadOnlyList<string> itemKeys,
        RefreshMode moduleDefault)
    {
        if (!IsGroupRefreshEnabled(moduleId))
        {
            return RefreshMode.Manual;
        }

        // With no items declared in config, honour the module-level default.
        if (itemKeys.Count == 0)
        {
            return moduleDefault;
        }

        return itemKeys.Any(k => GetEffectiveItemRefreshMode(moduleId, k, moduleDefault) == RefreshMode.Auto)
            ? RefreshMode.Auto
            : RefreshMode.Manual;
    }

    /// <summary>
    /// Minimum effective interval in seconds across all Auto items for the module,
    /// used to compute the polling tick rate.
    /// </summary>
    public int GetModuleEffectiveIntervalSeconds(
        string moduleId,
        IReadOnlyList<string> itemKeys,
        int moduleDefaultSeconds,
        RefreshMode moduleDefault)
    {
        if (!IsGroupRefreshEnabled(moduleId) || itemKeys.Count == 0)
        {
            return moduleDefaultSeconds;
        }

        return itemKeys
            .Where(k => GetEffectiveItemRefreshMode(moduleId, k, moduleDefault) == RefreshMode.Auto)
            .Select(k => GetEffectiveItemRefreshInterval(moduleId, k, moduleDefaultSeconds))
            .DefaultIfEmpty(moduleDefaultSeconds)
            .Min();
    }

    // ── Persistence ────────────────────────────────────────────────────────────

    private DashboardUserSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new DashboardUserSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<DashboardUserSettings>(json, JsonOptions) ?? new DashboardUserSettings();
        }
        catch
        {
            return new DashboardUserSettings();
        }
    }

    private async Task SaveAsync()
    {
        await saveLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(settingsPath, json);
        }
        finally
        {
            saveLock.Release();
        }
    }
}
