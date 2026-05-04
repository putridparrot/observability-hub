using Dashboard.Web.Core.Models;

namespace Dashboard.Web.Core.Abstractions;

public interface IDashboardModule
{
    string Id { get; }

    string DisplayName { get; }

    string Description { get; }

    /// <summary>Whether this module's widgets appear on the main dashboard page by default.</summary>
    bool ShowOnDashboard { get; }

    /// <summary>Name of the dedicated resource page for this module. Defaults to DisplayName when null.</summary>
    string? PageGroup { get; }

    /// <summary>Auto re-collects on a timer; Manual only re-collects on explicit trigger.</summary>
    RefreshMode RefreshMode { get; }

    /// <summary>Interval between automatic re-collections. Ignored when RefreshMode is Manual.</summary>
    TimeSpan RefreshInterval { get; }

    /// <summary>Known item keys from configuration (e.g. account names, endpoint names).
    /// Used by the Settings page before any collection runs.</summary>
    IReadOnlyList<string> ItemKeys { get; }

    Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken);
}

public static class DashboardModuleExtensions
{
    /// <summary>Returns PageGroup if set, otherwise falls back to DisplayName.
    /// Every module gets a resource page; this is the nav/routing key.</summary>
    public static string GetEffectivePageGroup(this IDashboardModule module) =>
        module.PageGroup ?? module.DisplayName;
}