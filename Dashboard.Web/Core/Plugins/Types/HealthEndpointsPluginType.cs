using System.Text.Json.Nodes;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Modules;
using Dashboard.Web.Core.Options;

namespace Dashboard.Web.Core.Plugins.Types;

public sealed class HealthEndpointsPluginType(IHttpClientFactory httpClientFactory) : IPluginType
{
    public string TypeId => "health-endpoints";
    public string DisplayName => "Health Endpoints";
    public string Description => "Checks configured App Service or Kubernetes health endpoints.";

    public IReadOnlyList<PluginField> Fields =>
    [
        new("displayName", "Display Name", PluginFieldType.Text, Required: true, DefaultValue: "Health Endpoints"),
        new("showOnDashboard", "Show on Dashboard", PluginFieldType.Boolean, DefaultValue: "true"),
        new("pageGroup", "Page Group", PluginFieldType.Text, Placeholder: "e.g. Monitoring",
            Help: "Groups this module on a dedicated resource page. Leave blank to use the display name."),
        new("refreshMode", "Refresh Mode", PluginFieldType.Select, SelectOptions: ["Auto", "Manual"], DefaultValue: "Auto"),
        new("refreshIntervalSeconds", "Refresh Interval (s)", PluginFieldType.Number, DefaultValue: "30"),
        new("timeoutSeconds", "HTTP Timeout (s)", PluginFieldType.Number, DefaultValue: "10"),
        new("endpoints", "Endpoints", PluginFieldType.ItemList,
            Help: "Add each service URL to check.",
            ItemFields:
            [
                new("name", "Name", PluginFieldType.Text, Placeholder: "My Service"),
                new("url", "URL", PluginFieldType.Url, Required: true, Placeholder: "https://my-service/health")
            ])
    ];

    public IDashboardModule CreateModule(PluginInstanceConfig config)
    {
        var c = config.Config;
        var opts = new HealthEndpointOptions
        {
            Display = new ModuleDisplayOptions
            {
                DisplayName = PluginInstancesStore.ReadString(c["displayName"], "Health Endpoints"),
                ShowOnDashboard = PluginInstancesStore.ReadBool(c["showOnDashboard"], true),
                PageGroup = string.IsNullOrWhiteSpace(c["pageGroup"]?.ToString()) ? null : c["pageGroup"]!.ToString(),
                RefreshMode = Enum.TryParse<RefreshMode>(PluginInstancesStore.ReadString(c["refreshMode"], "Auto"), out var rm) ? rm : RefreshMode.Auto,
                RefreshIntervalSeconds = PluginInstancesStore.ReadInt(c["refreshIntervalSeconds"], 30)
            },
            TimeoutSeconds = PluginInstancesStore.ReadInt(c["timeoutSeconds"], 10),
            Endpoints = c["endpoints"]?.AsArray()?.OfType<JsonObject>().Select(e => new HealthEndpointConfig
            {
                Name = PluginInstancesStore.ReadString(e["name"]),
                Url = PluginInstancesStore.ReadString(e["url"])
            }).ToList() ?? []
        };
        return new HealthEndpointsModule(config.InstanceId, httpClientFactory, opts);
    }

    public PluginInstanceConfig CreateDefault() => new()
    {
        TypeId = TypeId,
        Name = "Health Endpoints",
        Enabled = true,
        Config = new JsonObject
        {
            ["displayName"] = "Health Endpoints",
            ["showOnDashboard"] = true,
            ["pageGroup"] = JsonNode.Parse("null"),
            ["refreshMode"] = "Auto",
            ["refreshIntervalSeconds"] = 30,
            ["timeoutSeconds"] = 10,
            ["endpoints"] = new JsonArray()
        }
    };
}
