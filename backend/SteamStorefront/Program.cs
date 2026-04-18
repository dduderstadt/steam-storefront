using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using SteamStorefront.Data;
using SteamStorefront.Jobs;
using SteamStorefront.Services;
using SteamStorefront.Steam;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton<ICacheService, CacheService>();

builder.Services.AddHttpClient<ISteamApiClient, SteamApiClient>();

builder.Services.AddScoped<ILibraryService, LibraryService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<ISyncService, SyncService>();

builder.Services.AddHostedService<LibrarySyncJob>();

builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.MapControllers();

app.Run();

public partial class Program { }
