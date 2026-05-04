namespace Dashboard.Web.Core.Models;

public enum RefreshMode
{
    /// <summary>Background service re-collects on the configured interval.</summary>
    Auto,

    /// <summary>Data is only collected on startup and when explicitly triggered.</summary>
    Manual
}
