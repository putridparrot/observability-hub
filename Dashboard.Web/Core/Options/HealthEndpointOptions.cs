namespace Dashboard.Web.Core.Options;

public sealed class HealthEndpointOptions
{
    public const string SectionName = "DashboardModules:HealthEndpoints";

    public ModuleDisplayOptions Display { get; set; } = new();

    public int TimeoutSeconds { get; set; } = 10;

    public List<HealthEndpointConfig> Endpoints { get; set; } = [];
}

public sealed class HealthEndpointConfig
{
    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
}