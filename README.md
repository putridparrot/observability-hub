# Dashboard

Pluggable operations dashboard built with Blazor and ASP.NET Core.

This project is designed so that new monitoring functionality can be added as modules without changing the dashboard UI page.

## Plugin model overview

A plugin is any class that implements the shared module contract in [Dashboard.Web/Core/Abstractions/IDashboardModule.cs](Dashboard.Web/Core/Abstractions/IDashboardModule.cs).

Each plugin returns one or more widgets.
The dashboard page renders all widgets from all registered modules automatically.

Key runtime pieces:

- Module contract: [Dashboard.Web/Core/Abstractions/IDashboardModule.cs](Dashboard.Web/Core/Abstractions/IDashboardModule.cs)
- Widget models: [Dashboard.Web/Core/Models/DashboardWidget.cs](Dashboard.Web/Core/Models/DashboardWidget.cs), [Dashboard.Web/Core/Models/DashboardMetric.cs](Dashboard.Web/Core/Models/DashboardMetric.cs), [Dashboard.Web/Core/Models/WidgetStatus.cs](Dashboard.Web/Core/Models/WidgetStatus.cs)
- Polling and cache: [Dashboard.Web/Core/Services/DashboardDataService.cs](Dashboard.Web/Core/Services/DashboardDataService.cs), [Dashboard.Web/Core/Services/ModulePollingService.cs](Dashboard.Web/Core/Services/ModulePollingService.cs)
- UI rendering: [Dashboard.Web/Components/Pages/Home.razor](Dashboard.Web/Components/Pages/Home.razor)

## How to add a plugin

1. Create a module class in [Dashboard.Web/Core/Modules](Dashboard.Web/Core/Modules).
2. Implement IDashboardModule and return widgets from CollectAsync.
3. Register the module in [Dashboard.Web/Program.cs](Dashboard.Web/Program.cs).
4. Add config in [Dashboard.Web/appsettings.json](Dashboard.Web/appsettings.json) if the module needs settings.
5. Run and verify the new widget cards appear on the dashboard.

### Step 1: Create the module

Create a file in the modules folder, for example:

- Dashboard.Web/Core/Modules/MyCustomModule.cs (new file to create)

Example implementation:

```csharp
using Dashboard.Web.Core.Abstractions;
using Dashboard.Web.Core.Models;

namespace Dashboard.Web.Core.Modules;

public sealed class MyCustomModule : IDashboardModule
{
    public string Id => "my-custom-module";

    public string DisplayName => "My Custom Module";

    public string Description => "Example plugin that publishes one widget.";

    public TimeSpan RefreshInterval => TimeSpan.FromMinutes(1);

    public Task<IReadOnlyList<DashboardWidget>> CollectAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DashboardWidget> widgets =
        [
            new DashboardWidget(
                Id,
                DisplayName,
                "Sample Status",
                "Module is running",
                WidgetStatus.Healthy,
                DateTimeOffset.UtcNow,
                [new DashboardMetric("Version", "1.0")])
        ];

        return Task.FromResult(widgets);
    }
}
```

### Step 2: Register in DI

In [Dashboard.Web/Program.cs](Dashboard.Web/Program.cs), add a registration line:

```csharp
builder.Services.AddSingleton<IDashboardModule, MyCustomModule>();
```

Add this with the other module registrations.

### Step 3: Add module settings (optional)

If your module needs settings:

1. Create an options class under [Dashboard.Web/Core/Options](Dashboard.Web/Core/Options).
2. Bind it in [Dashboard.Web/Program.cs](Dashboard.Web/Program.cs).
3. Add values in [Dashboard.Web/appsettings.json](Dashboard.Web/appsettings.json).

Pattern reference modules:

- [Dashboard.Web/Core/Modules/HealthEndpointsModule.cs](Dashboard.Web/Core/Modules/HealthEndpointsModule.cs)
- [Dashboard.Web/Core/Modules/BlobLimitsModule.cs](Dashboard.Web/Core/Modules/BlobLimitsModule.cs)
- [Dashboard.Web/Core/Modules/AppInsightsKqlModule.cs](Dashboard.Web/Core/Modules/AppInsightsKqlModule.cs)

### Step 4: Run and verify

Local run:

```bash
dotnet run --project Dashboard.Web
```

Then open http://localhost:5000 or the URL shown by ASP.NET startup output.

Your module cards should appear automatically on the main dashboard page.

## Build and deployment references

- Local container run: [docker-compose.yml](docker-compose.yml)
- Container image build: [Dockerfile](Dockerfile)
- Kubernetes deployment notes: [k8s/README.md](k8s/README.md)

## Suggested plugin conventions

- Keep one concern per module.
- Return clear widget titles and short summaries.
- Prefer healthy, warning, and critical status consistently.
- Handle exceptions inside CollectAsync and return a warning or critical widget rather than throwing.
- Use options classes for all module-specific configuration.
