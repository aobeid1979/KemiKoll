using KemiKoll.Application.Products;
using KemiKoll.Infrastructure.Products;
using KemiKoll.Web.Components;
using KemiKoll.Web.Development;

var builder = WebApplication.CreateBuilder(args);

// Blazor with interactive server rendering.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Composition root: this is the only place that connects interfaces to implementations.
// Storage: temporary in-memory repository, one shared instance for the whole app.
// It is replaced by the database implementation in a later step; nothing else changes.
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// Use cases: a new instance per request.
builder.Services.AddScoped<AddProduct>();
builder.Services.AddScoped<ListProducts>();

var app = builder.Build();

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
