using SbClient.Web.Components;
using SbClient.Web.Models;
using SbClient.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.Configure<MudGatewayOptions>(builder.Configuration.GetSection(MudGatewayOptions.SectionName));
builder.Services.AddScoped<IMudSideChannelDecoder, NoopMudSideChannelDecoder>();
builder.Services.AddScoped<MudClientSession>();
builder.Services.AddScoped<BrowserLearningSnapshotFactory>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/learning/browser-boundary", (BrowserLearningSnapshotFactory factory) => factory.Create());

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SbClient.Web.Client._Imports).Assembly);

app.Run();
