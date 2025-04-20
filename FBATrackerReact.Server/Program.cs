// <copyright file="Program.cs" company="Talia Sales Ltd">
// Copyright (c) Talia Sales Ltd. All rights reserved.
// </copyright>

using System.Net;
using System.Reflection;
using FBATrackerReact.Server.Extensions.Program;
using FBATrackerReact.Server.Models.Configuration;
using FBATrackerReact.Server.Services.Database;
using FBATrackerReact.Server.Services.Logging;
using FBATrackerReact.Server.Services.Search;
using FBATrackerReact.SharedScraper.Services;
using FikaAmazonAPI;
using FlareSolverrSharp.Solvers;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Console;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var configuration = new Configuration();
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
builder.Configuration.GetSection("AppSettings").Bind(configuration);

var isProduction = builder.Environment.IsProduction();

builder.Services.AddLogging(builder =>
{
    var baseBuilder = builder
    .AddConsoleFormatter<LogFormatter, ConsoleFormatterOptions>(options =>
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    }).AddConsole(options =>
    {
        options.FormatterName = nameof(LogFormatter);
    }).AddProvider(new CustomLoggerProvider());

    if (!isProduction)
    {
        baseBuilder.AddDebug();
    }
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.WriteIndented = false;
});

var dbContextOptions = new DbContextOptionsBuilder<FbaDbContext>().UseNpgsql($"User ID={configuration.ConfiguredServices["Postgres"].Username};Password={configuration.ConfiguredServices["Postgres"].Password};Server={configuration.ConfiguredHosts["Postgres"]};Port=5432;Database=fba;Pooling=true;");
builder.Services.AddTransient<FbaDbContext>((_) => new FbaDbContext(dbContextOptions.Options));
builder.Services.AddTransient<IFbaDbContext>((sp) => sp.GetRequiredService<FbaDbContext>());

builder.Services.AddSingleton(new AmazonConnection(configuration.AmazonCredentials));

builder.Services.AddKeyedSingleton("SellerAmp", configuration.ConfiguredServices["SellerAmp"]);

builder.Services.AddSingleton<IFlurlClient>(new FlurlClient()
    .WithHeader("User-Agent", "PostmanRuntime/7.39.0")
    .WithHeader("Accept", "*/*")
    .WithTimeout(60));

builder.Services.AddSingleton<FlareSolverr>(new FlareSolverr(configuration.ConfiguredHosts["FlareSolverr"])
{
    MaxTimeout = 60000,
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddKeyedSingleton<IFlurlClient>("Proxy", (sp, sk) => sp.GetRequiredService<IFlurlClient>());
    builder.Services.AddKeyedSingleton<FlareSolverr>("Proxy", (sp, sk) => sp.GetRequiredService<FlareSolverr>());
}
else
{
    builder.Services.AddKeyedSingleton<IFlurlClient>("Proxy", new FlurlClientBuilder()
        .ConfigureInnerHandler(h =>
        {
            h.Proxy = new WebProxy(configuration.ConfiguredHosts["Tunnel"]);
            h.UseProxy = true;
        })
        .WithHeader("User-Agent", "PostmanRuntime/7.39.0")
        .WithHeader("Accept", "*/*").Build());

    builder.Services.AddKeyedSingleton("Proxy", new FlareSolverr(configuration.ConfiguredHosts["FlareSolverr"])
    {
        ProxyUrl = configuration.ConfiguredHosts["Tunnel"],
        MaxTimeout = 60000,
    });
}

builder.Services.AddSingleton<IRequestHelperService, RequestHelperService>();

builder.Services.AddSingleton<CatalogSearchService>();
builder.Services.AddSingleton<FbaSearchService>();
builder.Services.AddSingleton<PricingSearchService>();

builder.Services.RegisterScrapers();

builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

WebApplication app = builder.Build();

ILoggerFactory loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

using (logger.BeginScope("Migration"))
{
    try
    {
        var dbContext = app.Services.GetRequiredService<IFbaDbContext>();

        logger.LogInformation("Performing database migration");

        (dbContext as FbaDbContext).Database.Migrate();
        (dbContext as FbaDbContext).Database.ExecuteSqlRaw("REINDEX DATABASE fba");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed");
        throw;
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.UseResponseCaching();

app.MapFallbackToFile("/index.html");

app.Run();