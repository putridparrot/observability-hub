namespace Dashboard.Web.Core.Models;

/// <summary>User-configurable settings persisted to dashboardUserSettings.json.</summary>
public sealed class DashboardUserSettings
{
    /// <summary>
    /// Key = moduleId. When absent or true, background refresh is enabled for that module.
    /// Setting false treats the whole module as Manual — no background polling.
    /// </summary>
    public Dictionary<string, bool> GroupRefreshEnabled { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Key = "moduleId::itemKey". Stores per-item display and refresh overrides.
    /// </summary>
    public Dictionary<string, ItemSettings> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ItemSettings
{
    /// <summary>Whether this item appears on the main dashboard page.</summary>
    public bool ShowOnDashboard { get; set; } = true;

    /// <summary>"Default" inherits the module-level RefreshMode; "Auto" or "Manual" override it.</summary>
    public string RefreshMode { get; set; } = "Default";

    /// <summary>Override for refresh interval in seconds. Null = use the module-level default.</summary>
    public int? RefreshIntervalSeconds { get; set; }
}
