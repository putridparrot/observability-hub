using System.Text.Json.Nodes;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Dashboard.Web.Core.Modules;
using Dashboard.Web.Core.Options;

namespace Dashboard.Web.Core.Plugins.Types;

public sealed class BlobLimitsPluginType : IPluginType
{
    public string TypeId => "blob-limits";
    public string DisplayName => "Blob Storage";
    public string Description => "Tracks configured capacity and threshold values for blob storage accounts.";

    public IReadOnlyList<PluginField> Fields =>
    [
        new("displayName", "Display Name", PluginFieldType.Text, Required: true, DefaultValue: "Blob Storage"),
        new("showOnDashboard", "Show on Dashboard", PluginFieldType.Boolean, DefaultValue: "true"),
        new("pageGroup", "Page Group", PluginFieldType.Text, Placeholder: "e.g. Storage",
            Help: "Groups this module on a dedicated resource page. Leave blank to use the display name."),
        new("refreshMode", "Refresh Mode", PluginFieldType.Select, SelectOptions: ["Auto", "Manual"], DefaultValue: "Auto"),
        new("refreshIntervalSeconds", "Refresh Interval (s)", PluginFieldType.Number, DefaultValue: "60"),
        new("accounts", "Storage Accounts", PluginFieldType.ItemList,
            Help: "Configure each storage account with its current usage and limits.",
            ItemFields:
            [
                new("name", "Account Name", PluginFieldType.Text, Required: true, Placeholder: "my-storage"),
                new("usedGiB", "Used (GiB)", PluginFieldType.Number, DefaultValue: "0"),
                new("limitGiB", "Limit (GiB)", PluginFieldType.Number, DefaultValue: "100"),
                new("warningPercent", "Warn %", PluginFieldType.Number, DefaultValue: "80"),
                new("criticalPercent", "Critical %", PluginFieldType.Number, DefaultValue: "95")
            ])
    ];

    public IDashboardModule CreateModule(PluginInstanceConfig config)
    {
        var c = config.Config;
        var opts = new BlobLimitsOptions
        {
            Display = new ModuleDisplayOptions
            {
                DisplayName = PluginInstancesStore.ReadString(c["displayName"], "Blob Storage"),
                ShowOnDashboard = PluginInstancesStore.ReadBool(c["showOnDashboard"], true),
                PageGroup = string.IsNullOrWhiteSpace(c["pageGroup"]?.ToString()) ? null : c["pageGroup"]!.ToString(),
                RefreshMode = Enum.TryParse<RefreshMode>(PluginInstancesStore.ReadString(c["refreshMode"], "Auto"), out var rm) ? rm : RefreshMode.Auto,
                RefreshIntervalSeconds = PluginInstancesStore.ReadInt(c["refreshIntervalSeconds"], 60)
            },
            Accounts = c["accounts"]?.AsArray()?.OfType<JsonObject>().Select(a => new BlobAccountConfig
            {
                Name = PluginInstancesStore.ReadString(a["name"]),
                UsedGiB = PluginInstancesStore.ReadDouble(a["usedGiB"]),
                LimitGiB = PluginInstancesStore.ReadDouble(a["limitGiB"], 100),
                WarningPercent = PluginInstancesStore.ReadDouble(a["warningPercent"], 80),
                CriticalPercent = PluginInstancesStore.ReadDouble(a["criticalPercent"], 95)
            }).ToList() ?? []
        };
        return new BlobLimitsModule(config.InstanceId, opts);
    }

    public PluginInstanceConfig CreateDefault() => new()
    {
        TypeId = TypeId,
        Name = "Blob Storage",
        Enabled = true,
        Config = new JsonObject
        {
            ["displayName"] = "Blob Storage",
            ["showOnDashboard"] = true,
            ["pageGroup"] = "Storage",
            ["refreshMode"] = "Auto",
            ["refreshIntervalSeconds"] = 60,
            ["accounts"] = new JsonArray()
        }
    };
}
