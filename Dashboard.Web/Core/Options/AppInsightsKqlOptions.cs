namespace Dashboard.Web.Core.Options;

public sealed class AppInsightsKqlOptions
{
    public const string SectionName = "DashboardModules:AppInsightsKql";

    public ModuleDisplayOptions Display { get; set; } = new();

    public bool Enabled { get; set; }

    public string Title { get; set; } = "Application Insights Exception Watch";

    public string AppId { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string Query { get; set; } = "exceptions | where type == \"System.InvalidOperationException\" | summarize Count=count()";

    public string Timespan { get; set; } = "PT24H";

    public int MaxAllowedMatches { get; set; }
}