using Dashboard.Web.Components;
using Dashboard.Web.Core.Plugins;
using Dashboard.Web.Core.Plugins.Types;
using Dashboard.Web.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpClient();

// Plugin types — each describes a category of dashboard module.
builder.Services.AddSingleton<IPluginType, BlobLimitsPluginType>();
builder.Services.AddSingleton<IPluginType, HealthEndpointsPluginType>();
builder.Services.AddSingleton<IPluginType, AppInsightsKqlPluginType>();

// Stores plugin instances to pluginInstances.json; seeds defaults on first run.
builder.Services.AddSingleton<PluginInstancesStore>();

builder.Services.AddSingleton<UserSettingsService>();
builder.Services.AddSingleton<DashboardDataService>();
builder.Services.AddHostedService<ModulePollingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
