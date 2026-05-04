using System.Text.Json;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Options;
using Microsoft.AspNetCore.WebUtilities;

namespace Dashboard.Web.Core.Modules;

public sealed class AppInsightsKqlModule(
    string instanceId,
    IHttpClientFactory httpClientFactory,
    AppInsightsKqlOptions options) : IDashboardModule
{
    public string Id => instanceId;

    public string DisplayName => string.IsNullOrWhiteSpace(options.Display.DisplayName)
        ? "Application Insights" : options.Display.DisplayName;

    public string Description => "Runs a periodic KQL query to detect new exceptions or trace patterns.";

    public bool ShowOnDashboard => options.Display.ShowOnDashboard;

    public string? PageGroup => options.Display.PageGroup;

    public RefreshMode RefreshMode => options.Display.RefreshMode;

    public TimeSpan RefreshInterval => TimeSpan.FromSeconds(options.Display.RefreshIntervalSeconds);

    public IReadOnlyList<string> ItemKeys => [options.Title];

    public async Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    options.Title,
                    "Module disabled. Enable it via the Plugins page.",
                    WidgetStatus.Unknown,
                    DateTimeOffset.UtcNow,
                    ItemKey: options.Title)
            ];
        }

        if (string.IsNullOrWhiteSpace(options.AppId) || string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    options.Title,
                    "Missing AppId or ApiKey configuration.",
                    WidgetStatus.Warning,
                    DateTimeOffset.UtcNow,
                    ItemKey: options.Title)
            ];
        }

        var client = httpClientFactory.CreateClient(nameof(AppInsightsKqlModule));
        var requestUri = QueryHelpers.AddQueryString(
            $"https://api.applicationinsights.io/v1/apps/{options.AppId}/query",
            new Dictionary<string, string?>
            {
                ["query"] = options.Query,
                ["timespan"] = options.Timespan
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("x-api-key", options.ApiKey);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return
                [
                    new DashboardWidget(
                        Id,
                        DisplayName,
                        options.Title,
                        $"KQL failed with HTTP {(int)response.StatusCode}",
                        WidgetStatus.Warning,
                        DateTimeOffset.UtcNow,
                        [new DashboardMetric("Response", payload.Length > 120 ? payload[..120] + "..." : payload)],
                        ItemKey: options.Title)
                ];
            }

            var matchCount = ReadMatchCount(payload);
            var status = matchCount > options.MaxAllowedMatches ? WidgetStatus.Critical : WidgetStatus.Healthy;

            return
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    options.Title,
                    $"{matchCount} matches in {options.Timespan}",
                    status,
                    DateTimeOffset.UtcNow,
                    [
                        new DashboardMetric("Matches", matchCount.ToString()),
                        new DashboardMetric("Threshold", options.MaxAllowedMatches.ToString())
                    ],
                    ItemKey: options.Title)
            ];
        }
        catch (Exception ex)
        {
            return
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    options.Title,
                    ex.Message,
                    WidgetStatus.Warning,
                    DateTimeOffset.UtcNow,
                    ItemKey: options.Title)
            ];
        }
    }

    private static int ReadMatchCount(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        if (!document.RootElement.TryGetProperty("tables", out var tables) || tables.GetArrayLength() == 0)
        {
            return 0;
        }

        var firstTable = tables[0];
        if (!firstTable.TryGetProperty("rows", out var rows) || rows.GetArrayLength() == 0)
        {
            return 0;
        }

        var firstValue = rows[0][0];
        if (firstValue.ValueKind == JsonValueKind.Number && firstValue.TryGetInt32(out var value))
        {
            return value;
        }

        return rows.GetArrayLength();
    }
}