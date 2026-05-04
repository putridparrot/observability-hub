using System.Text.Json.Nodes;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Modules;
using Dashboard.Web.Core.Options;

namespace Dashboard.Web.Core.Plugins.Types;

public sealed class AppInsightsKqlPluginType(IHttpClientFactory httpClientFactory) : IPluginType
{
    public string TypeId => "appinsights-kql";
    public string DisplayName => "Application Insights KQL";
    public string Description => "Runs a periodic KQL query against Application Insights to detect patterns or exceptions.";

    public IReadOnlyList<PluginField> Fields =>
    [
        new("displayName", "Display Name", PluginFieldType.Text, Required: true, DefaultValue: "Application Insights KQL"),
        new("showOnDashboard", "Show on Dashboard", PluginFieldType.Boolean, DefaultValue: "true"),
        new("pageGroup", "Page Group", PluginFieldType.Text, Placeholder: "e.g. Monitoring",
            Help: "Groups this module on a dedicated resource page. Leave blank to use the display name."),
        new("refreshMode", "Refresh Mode", PluginFieldType.Select, SelectOptions: ["Auto", "Manual"], DefaultValue: "Auto"),
        new("refreshIntervalSeconds", "Refresh Interval (s)", PluginFieldType.Number, DefaultValue: "300"),
        new("enabled", "Enabled", PluginFieldType.Boolean, DefaultValue: "false",
            Help: "Disable to suppress polling without removing the configuration."),
        new("title", "Widget Title", PluginFieldType.Text, Required: true,
            DefaultValue: "Application Insights Exception Watch",
            Placeholder: "e.g. Exception Monitor"),
        new("appId", "App ID", PluginFieldType.Text, Required: true,
            Placeholder: "Application Insights application ID",
            Help: "Found in the Application Insights resource overview."),
        new("apiKey", "API Key", PluginFieldType.Password,
            Help: "Create a read-only API key in Application Insights → API Access."),
        new("query", "KQL Query", PluginFieldType.Textarea,
            DefaultValue: "exceptions | where type == \"System.InvalidOperationException\" | summarize Count=count()",
            Placeholder: "exceptions | summarize count()"),
        new("timespan", "Timespan", PluginFieldType.Text, DefaultValue: "PT24H",
            Placeholder: "ISO 8601 duration, e.g. PT1H",
            Help: "ISO 8601 duration for the query time window."),
        new("maxAllowedMatches", "Max Allowed Matches", PluginFieldType.Number, DefaultValue: "0",
            Help: "Alert as Critical when row count exceeds this value. 0 = alert on any matches.")
    ];

    public IDashboardModule CreateModule(PluginInstanceConfig config)
    {
        var c = config.Config;
        var opts = new AppInsightsKqlOptions
        {
            Display = new ModuleDisplayOptions
            {
                DisplayName = PluginInstancesStore.ReadString(c["displayName"], "Application Insights KQL"),
                ShowOnDashboard = PluginInstancesStore.ReadBool(c["showOnDashboard"], true),
                PageGroup = string.IsNullOrWhiteSpace(c["pageGroup"]?.ToString()) ? null : c["pageGroup"]!.ToString(),
                RefreshMode = Enum.TryParse<RefreshMode>(PluginInstancesStore.ReadString(c["refreshMode"], "Auto"), out var rm) ? rm : RefreshMode.Auto,
                RefreshIntervalSeconds = PluginInstancesStore.ReadInt(c["refreshIntervalSeconds"], 300)
            },
            Enabled = PluginInstancesStore.ReadBool(c["enabled"]),
            Title = PluginInstancesStore.ReadString(c["title"], "Application Insights Exception Watch"),
            AppId = PluginInstancesStore.ReadString(c["appId"]),
            ApiKey = PluginInstancesStore.ReadString(c["apiKey"]),
            Query = PluginInstancesStore.ReadString(c["query"], "exceptions | summarize count()"),
            Timespan = PluginInstancesStore.ReadString(c["timespan"], "PT24H"),
            MaxAllowedMatches = PluginInstancesStore.ReadInt(c["maxAllowedMatches"])
        };
        return new AppInsightsKqlModule(config.InstanceId, httpClientFactory, opts);
    }

    public PluginInstanceConfig CreateDefault() => new()
    {
        TypeId = TypeId,
        Name = "Application Insights",
        Enabled = true,
        Config = new JsonObject
        {
            ["displayName"] = "Application Insights",
            ["showOnDashboard"] = true,
            ["pageGroup"] = JsonNode.Parse("null"),
            ["refreshMode"] = "Auto",
            ["refreshIntervalSeconds"] = 300,
            ["enabled"] = false,
            ["title"] = "Application Insights Exception Watch",
            ["appId"] = string.Empty,
            ["apiKey"] = string.Empty,
            ["query"] = "exceptions | where type == \"System.InvalidOperationException\" | summarize Count=count()",
            ["timespan"] = "PT24H",
            ["maxAllowedMatches"] = 0
        }
    };
}
