namespace Dashboard.Web.Core.Options;

public sealed class BlobLimitsOptions
{
    public const string SectionName = "DashboardModules:BlobLimits";

    public ModuleDisplayOptions Display { get; set; } = new();

    public List<BlobAccountConfig> Accounts { get; set; } = [];
}

public sealed class BlobAccountConfig
{
    public string Name { get; set; } = string.Empty;

    public double UsedGiB { get; set; }

    public double LimitGiB { get; set; }

    public double WarningPercent { get; set; } = 80;

    public double CriticalPercent { get; set; } = 95;
}