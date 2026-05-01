using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using SteamStorefront.Data;
using SteamStorefront.Jobs;
using SteamStorefront.Middleware;
using SteamStorefront.Services;
using SteamStorefront.Steam;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// PostgreSQL via EF Core. Connection string comes from appsettings / env vars (never hardcoded).
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Redis as a singleton — the multiplexer is thread-safe and designed to be shared.
// CacheService wraps it so the rest of the app never takes a direct StackExchange.Redis dependency.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton<ICacheService, CacheService>();

// Typed HttpClient — ASP.NET Core injects the configured HttpClient into SteamApiClient's constructor.
builder.Services.AddHttpClient<ISteamApiClient, SteamApiClient>();

// Scoped services: a new instance per HTTP request (or per sync job scope).
builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<ISyncService, SyncService>();

// Background sync job — runs for the lifetime of the application.
builder.Services.AddHostedService<LibrarySyncJob>();

// CORS: allow requests from the frontend origin. Defaults to localhost:3000 for local dev.
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Run migrations automatically on startup.
// Skipped for the InMemory provider used in tests — InMemory doesn't support migrations.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Must be first — wraps the entire pipeline so no exception can escape unhandled.
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.MapControllers();

app.Run();

// Partial class declaration makes Program visible to WebApplicationFactory in integration tests.
public partial class Program { }
