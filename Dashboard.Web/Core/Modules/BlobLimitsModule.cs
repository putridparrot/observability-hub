using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Options;

namespace Dashboard.Web.Core.Modules;

public sealed class BlobLimitsModule(string instanceId, BlobLimitsOptions options) : IDashboardModule
{
    public string Id => instanceId;

    public string DisplayName => string.IsNullOrWhiteSpace(options.Display.DisplayName)
        ? "Blob Storage" : options.Display.DisplayName;

    public string Description => "Tracks configured capacity and threshold values for blob storage accounts.";

    public bool ShowOnDashboard => options.Display.ShowOnDashboard;

    public string? PageGroup => options.Display.PageGroup;

    public RefreshMode RefreshMode => options.Display.RefreshMode;

    public TimeSpan RefreshInterval => TimeSpan.FromSeconds(options.Display.RefreshIntervalSeconds);

    public IReadOnlyList<string> ItemKeys => options.Accounts.Select(a => a.Name).ToList();

    public Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken)
    {
        if (options.Accounts.Count == 0)
        {
            IReadOnlyList<DashboardWidget> notConfigured =
                [
                    new DashboardWidget(
                        Id,
                        DisplayName,
                        "No accounts configured",
                        "Add storage accounts via the Plugins page.",
                        WidgetStatus.Unknown,
                        DateTimeOffset.UtcNow,
                        ItemKey: "no-accounts")
                ];

            return Task.FromResult(notConfigured);
        }

        var widgets = new List<DashboardWidget>(options.Accounts.Count);
        foreach (var account in options.Accounts)
        {
            var usagePercent = account.LimitGiB <= 0 ? 0 : account.UsedGiB / account.LimitGiB * 100;
            var status = usagePercent >= account.CriticalPercent
                ? WidgetStatus.Critical
                : usagePercent >= account.WarningPercent
                    ? WidgetStatus.Warning
                    : WidgetStatus.Healthy;

            widgets.Add(new DashboardWidget(
                Id,
                DisplayName,
                account.Name,
                $"{usagePercent:0.0}% of configured limit in use",
                status,
                DateTimeOffset.UtcNow,
                [
                    new DashboardMetric("Used", $"{account.UsedGiB:0.##} GiB"),
                    new DashboardMetric("Limit", $"{account.LimitGiB:0.##} GiB"),
                    new DashboardMetric("Warning", $">= {account.WarningPercent:0.#}%"),
                    new DashboardMetric("Critical", $">= {account.CriticalPercent:0.#}%")
                ],
                ItemKey: account.Name));
        }

        return Task.FromResult<IReadOnlyList<DashboardWidget>>(widgets);
    }
}