using KemiKoll.Application.Documents;
using KemiKoll.Application.Products;
using KemiKoll.Application.SafetyDataSheets;
using KemiKoll.Infrastructure;
using KemiKoll.Infrastructure.Persistence;
using KemiKoll.Web;
using KemiKoll.Web.Components;
using KemiKoll.Web.Development;

var builder = WebApplication.CreateBuilder(args);

// Blazor with interactive server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Composition root: this is the only place that connects interfaces to implementations.
// Storage: PostgreSQL when the connection string "KemiKoll" is configured
// (in Development: User Secrets), otherwise in memory (data is lost on restart).
var connectionString = builder.Configuration.GetConnectionString("KemiKoll");
builder.Services.AddInfrastructure(connectionString);

// The real system clock. Use cases ask TimeProvider for "today" so tests can use a fixed date.
builder.Services.AddSingleton(TimeProvider.System);

// Use cases: a new instance per request.
builder.Services.AddScoped<AddProduct>();
builder.Services.AddScoped<ListProducts>();
builder.Services.AddScoped<EditProduct>();
builder.Services.AddScoped<AddSafetyDataSheet>();
builder.Services.AddScoped<ManageDataSheets>();
builder.Services.AddScoped<ReadSafetyDataSheet>();

var app = builder.Build();

StartupLog.Storage(
    app.Logger,
    string.IsNullOrWhiteSpace(connectionString) ? "in memory (data is lost on restart)" : "PostgreSQL");

// Check the database once at startup. If it cannot be used, stop with one clear message
// instead of failing later with a long technical error on the first page.
var database = await app.Services.CheckDatabaseAsync();
switch (database.State)
{
    case DatabaseState.Unreachable:
        StartupLog.DatabaseUnreachable(app.Logger, database.Server);
        Environment.ExitCode = 1;
        return;
    case DatabaseState.PendingMigrations:
        StartupLog.PendingMigrations(app.Logger, string.Join(", ", database.PendingMigrations));
        Environment.ExitCode = 1;
        return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Example products so the list is not empty during development.
    await SampleData.SeedAsync(app.Services);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
