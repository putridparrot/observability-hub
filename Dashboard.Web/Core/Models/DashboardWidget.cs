namespace Dashboard.Web.Core.Models;

public sealed record DashboardWidget(
    string ModuleId,
    string ModuleName,
    string Title,
    string Summary,
    WidgetStatus Status,
    DateTimeOffset TimestampUtc,
    IReadOnlyList<DashboardMetric>? Metrics = null,
    /// <summary>Stable identifier for this item within its module (e.g. account name, endpoint name).</summary>
    string ItemKey = "");