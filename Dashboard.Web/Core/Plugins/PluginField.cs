namespace Dashboard.Web.Core.Plugins;

public enum PluginFieldType
{
    Text,
    Password,
    Url,
    Textarea,
    Number,
    Boolean,
    Select,
    ItemList
}

/// <summary>
/// Describes a single configurable field for a plugin type.
/// Used to drive the dynamic configuration UI.
/// </summary>
public sealed record PluginField(
    string Key,
    string Label,
    PluginFieldType Type,
    string? Placeholder = null,
    string? DefaultValue = null,
    bool Required = false,
    string? Help = null,
    string[]? SelectOptions = null,
    PluginField[]? ItemFields = null);
