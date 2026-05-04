using Dashboard.Web.Core.Models;

namespace Dashboard.Web.Core.Options;

/// <summary>
/// Controls where and how a module surfaces its widgets.
/// Embed this in each module's options class and bind from configuration.
/// </summary>
public sealed class ModuleDisplayOptions
{
    /// <summary>User-facing name shown in navigation and page headers. Falls back to the plugin type default if empty.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>When true the module's widgets appear on the main Dashboard page.</summary>
    public bool ShowOnDashboard { get; set; } = true;

    /// <summary>
    /// Name of the dedicated resource page that lists this module's widgets in full.
    /// Leave null to only show on the dashboard.
    /// Multiple modules sharing the same value appear together on one page.
    /// </summary>
    public string? PageGroup { get; set; }

    /// <summary>Auto re-collects on <see cref="RefreshIntervalSeconds"/>; Manual only re-collects when triggered.</summary>
    public RefreshMode RefreshMode { get; set; } = RefreshMode.Auto;

    /// <summary>Seconds between automatic re-collections when <see cref="RefreshMode"/> is Auto.</summary>
    public int RefreshIntervalSeconds { get; set; } = 30;
}
