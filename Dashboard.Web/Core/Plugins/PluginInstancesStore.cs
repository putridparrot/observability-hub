using System.Text.Json;
using System.Text.Json.Nodes;
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Dashboard.Web.Core.Plugins;

/// <summary>
/// Singleton store for plugin instances. Persists to pluginInstances.json in the content root.
/// Seeds defaults on first run.
/// </summary>
public sealed class PluginInstancesStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, IPluginType> _pluginTypes;
    private readonly JsonSerializerOptions _jsonOpts;
    private readonly ILogger<PluginInstancesStore> _logger;
    private PluginInstancesFile _file;
    private readonly object _lock = new();

    /// <summary>Fired after any add/update/remove operation.</summary>
    public event Action? Changed;

    public PluginInstancesStore(
        IWebHostEnvironment env,
        IEnumerable<IPluginType> pluginTypes,
        ILogger<PluginInstancesStore> logger)
    {
        _filePath = Path.Combine(env.ContentRootPath, "pluginInstances.json");
        _pluginTypes = pluginTypes.ToDictionary(t => t.TypeId);
        _logger = logger;
        _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _file = Load();
    }

    public IReadOnlyList<PluginInstanceConfig> GetAllInstances()
    {
        lock (_lock)
        {
            return _file.Instances.ToList();
        }
    }

    public IReadOnlyList<IDashboardModule> GetAllModules()
    {
        lock (_lock)
        {
            var modules = new List<IDashboardModule>();
            foreach (var instance in _file.Instances.Where(i => i.Enabled))
            {
                if (_pluginTypes.TryGetValue(instance.TypeId, out var pluginType))
                {
                    try
                    {
                        modules.Add(pluginType.CreateModule(instance));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Failed to create dashboard module for plugin instance {InstanceId} of type {TypeId}.",
                            instance.InstanceId,
                            instance.TypeId);

                        modules.Add(new BrokenPluginModule(
                            instance,
                            $"Failed to load plugin configuration: {ex.Message}"));
                    }
                }
                else
                {
                    var reason = $"Plugin type '{instance.TypeId}' is not registered.";
                    _logger.LogWarning(
                        "Configured plugin instance {InstanceId} references unknown plugin type {TypeId}.",
                        instance.InstanceId,
                        instance.TypeId);
                    modules.Add(new BrokenPluginModule(instance, reason));
                }
            }
            return modules;
        }
    }

    public void AddInstance(PluginInstanceConfig instance)
    {
        lock (_lock)
        {
            _file.Instances.Add(instance);
            Save(_file);
        }
        Changed?.Invoke();
    }

    public void UpdateInstance(PluginInstanceConfig instance)
    {
        lock (_lock)
        {
            var idx = _file.Instances.FindIndex(i => i.InstanceId == instance.InstanceId);
            if (idx >= 0)
            {
                _file.Instances[idx] = instance;
                Save(_file);
            }
        }
        Changed?.Invoke();
    }

    public void RemoveInstance(string instanceId)
    {
        lock (_lock)
        {
            _file.Instances.RemoveAll(i => i.InstanceId == instanceId);
            Save(_file);
        }
        Changed?.Invoke();
    }

    public IPluginType? GetPluginType(string typeId) =>
        _pluginTypes.TryGetValue(typeId, out var t) ? t : null;

    public IReadOnlyList<IPluginType> GetPluginTypes() =>
        _pluginTypes.Values.ToList();

    /// <summary>
    /// Returns null when the configured instance can be materialized into a module;
    /// otherwise returns a human-readable error describing why it failed.
    /// </summary>
    public string? GetInstanceLoadError(PluginInstanceConfig instance)
    {
        lock (_lock)
        {
            if (!_pluginTypes.TryGetValue(instance.TypeId, out var pluginType))
            {
                return $"Plugin type '{instance.TypeId}' is not registered.";
            }

            try
            {
                _ = pluginType.CreateModule(instance);
                return null;
            }
            catch (Exception ex)
            {
                return $"Failed to load plugin configuration: {ex.Message}";
            }
        }
    }

    private PluginInstancesFile Load()
    {
        if (!File.Exists(_filePath))
        {
            var defaults = new PluginInstancesFile
            {
                Instances = _pluginTypes.Values.Select(t => t.CreateDefault()).ToList()
            };
            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PluginInstancesFile>(json, _jsonOpts) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private void Save(PluginInstancesFile file)
    {
        var json = JsonSerializer.Serialize(file, _jsonOpts);
        File.WriteAllText(_filePath, json);
    }

    /// <summary>Reads a number from a JsonNode tolerating both JSON number and string representations.</summary>
    internal static double ReadDouble(JsonNode? node, double fallback = 0)
    {
        if (node is null)
        {
            return fallback;
        }

        if (node.GetValueKind() == System.Text.Json.JsonValueKind.Number)
        {
            return double.TryParse(node.ToString(), out var parsed) ? parsed : fallback;
        }

        if (node.GetValueKind() == System.Text.Json.JsonValueKind.String)
        {
            return double.TryParse(node.GetValue<string>(), out var parsed) ? parsed : fallback;
        }

        return fallback;
    }

    internal static int ReadInt(JsonNode? node, int fallback = 0)
    {
        var d = ReadDouble(node, fallback);
        return (int)Math.Round(d);
    }

    internal static string ReadString(JsonNode? node, string fallback = "") =>
        node?.ToString() ?? fallback;

    internal static bool ReadBool(JsonNode? node, bool fallback = false) =>
        node?.GetValueKind() switch
        {
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            System.Text.Json.JsonValueKind.String => bool.TryParse(node.GetValue<string>(), out var b) ? b : fallback,
            _ => fallback
        };

    private sealed class BrokenPluginModule(PluginInstanceConfig instance, string reason) : IDashboardModule
    {
        public string Id => instance.InstanceId;

        public string DisplayName => string.IsNullOrWhiteSpace(instance.Name)
            ? $"{instance.TypeId} (Invalid)"
            : $"{instance.Name} (Invalid)";

        public string Description => "This plugin instance could not be loaded.";

        public bool ShowOnDashboard => true;

        public string? PageGroup => "Plugins";

        public RefreshMode RefreshMode => RefreshMode.Manual;

        public TimeSpan RefreshInterval => TimeSpan.FromMinutes(1);

        public IReadOnlyList<string> ItemKeys => ["plugin-error"];

        public Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<DashboardWidget> widgets =
            [
                new DashboardWidget(
                    Id,
                    DisplayName,
                    "Plugin configuration error",
                    reason,
                    WidgetStatus.Critical,
                    DateTimeOffset.UtcNow,
                    ItemKey: "plugin-error")
            ];

            return Task.FromResult(widgets);
        }
    }
}
