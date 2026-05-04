using Dashboard.Web.Core.Abstractions;

namespace Dashboard.Web.Core.Plugins;

/// <summary>
/// A plugin type descriptor. Implementations are registered in DI and describe
/// a category of dashboard module (e.g. Blob Storage, Health Endpoints).
/// </summary>
public interface IPluginType
{
    /// <summary>Stable type identifier, e.g. "blob-limits".</summary>
    string TypeId { get; }

    string DisplayName { get; }

    string Description { get; }

    /// <summary>Schema of configurable fields, used to render the config form.</summary>
    IReadOnlyList<PluginField> Fields { get; }

    /// <summary>Creates a live dashboard module from a persisted instance config.</summary>
    IDashboardModule CreateModule(PluginInstanceConfig config);

    /// <summary>Returns a default (pre-seeded) instance config for this plugin type.</summary>
    PluginInstanceConfig CreateDefault();
}
