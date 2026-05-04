using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Options;

namespace Dashboard.Web.Core.Modules;

public sealed class HealthEndpointsModule(
    string instanceId,
    IHttpClientFactory httpClientFactory,
    HealthEndpointOptions options) : IDashboardModule
{
    public string Id => instanceId;

    public string DisplayName => string.IsNullOrWhiteSpace(options.Display.DisplayName)
        ? "Health Endpoints" : options.Display.DisplayName;

    public string Description => "Checks configured App Service or Kubernetes health endpoints.";

    public bool ShowOnDashboard => options.Display.ShowOnDashboard;

    public string? PageGroup => options.Display.PageGroup;

    public RefreshMode RefreshMode => options.Display.RefreshMode;

    public TimeSpan RefreshInterval => TimeSpan.FromSeconds(options.Display.RefreshIntervalSeconds);

    public IReadOnlyList<string> ItemKeys => options.Endpoints.Select(e => e.Name).ToList();

    public async Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken)
    {
        if (options.Endpoints.Count == 0)
        {
            return
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    "No endpoints configured",
                    "Add endpoint URLs via the Plugins page.",
                    WidgetStatus.Unknown,
                    DateTimeOffset.UtcNow,
                    ItemKey: "no-endpoints")
            ];
        }

        var widgets = new List<DashboardWidget>(options.Endpoints.Count);
        var client = httpClientFactory.CreateClient(nameof(HealthEndpointsModule));
        client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));

        foreach (var endpoint in options.Endpoints)
        {
            var status = WidgetStatus.Critical;
            var summary = "No response";
            var elapsed = "n/a";

            if (!Uri.TryCreate(endpoint.Url, UriKind.Absolute, out var uri))
            {
                status = WidgetStatus.Warning;
                summary = "Invalid endpoint URL";
            }
            else
            {
                var started = DateTimeOffset.UtcNow;
                try
                {
                    using var response = await client.GetAsync(uri, cancellationToken);
                    var finished = DateTimeOffset.UtcNow;
                    elapsed = $"{(finished - started).TotalMilliseconds:0} ms";

                    if (response.IsSuccessStatusCode)
                    {
                        status = WidgetStatus.Healthy;
                        summary = $"HTTP {(int)response.StatusCode}";
                    }
                    else
                    {
                        status = WidgetStatus.Warning;
                        summary = $"HTTP {(int)response.StatusCode}";
                    }
                }
                catch (Exception ex)
                {
                    summary = ex.Message;
                }
            }

            widgets.Add(new DashboardWidget(
                Id,
                DisplayName,
                string.IsNullOrWhiteSpace(endpoint.Name) ? endpoint.Url : endpoint.Name,
                summary,
                status,
                DateTimeOffset.UtcNow,
                [new DashboardMetric("Latency", elapsed), new DashboardMetric("URL", endpoint.Url)],
                ItemKey: string.IsNullOrWhiteSpace(endpoint.Name) ? endpoint.Url : endpoint.Name));
        }

        return widgets;
    }
}