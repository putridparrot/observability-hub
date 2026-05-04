using System.Text.Json.Nodes;

namespace Dashboard.Web.Core.Plugins;

/// <summary>A single user-created instance of a plugin type, persisted to JSON.</summary>
public sealed class PluginInstanceConfig
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string TypeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>Type-specific configuration fields as a JSON object.</summary>
    public JsonObject Config { get; set; } = [];
}

/// <summary>Root object serialized to pluginInstances.json.</summary>
public sealed class PluginInstancesFile
{
    public List<PluginInstanceConfig> Instances { get; set; } = [];
}
