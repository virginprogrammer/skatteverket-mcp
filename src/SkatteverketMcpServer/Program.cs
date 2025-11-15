using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SkatteverketMcpServer;
using SkatteverketMcpServer.Prompts;
using SkatteverketMcpServer.Resources;
using SkatteverketMcpServer.Services;
using SkatteverketMcpServer.Tools;
using SkatteverketMcpServer.Transport;

// Configure Serilog for logging to file (avoiding stdout to prevent polluting stdio)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("logs/skatteverket-mcp-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Skatteverket MCP Server");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                  .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                  .AddUserSecrets<Program>(optional: true)
                  .AddEnvironmentVariables();
        })
        .ConfigureServices((context, services) =>
        {
            // Register configuration
            services.AddSingleton(context.Configuration);

            // Register HTTP client for Skatteverket API
            services.AddHttpClient<ISkatteverketApiClient, SkatteverketApiClient>()
                .ConfigureHttpClient((sp, client) =>
                {
                    var config = sp.GetRequiredService<IConfiguration>();
                    var baseUrl = config["Skatteverket:BaseUrl"] ?? "https://api.skatteverket.se";
                    client.BaseAddress = new Uri(baseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

            // Register MCP components
            services.AddSingleton<StdioTransport>();
            services.AddSingleton<VatDraftTools>();
            services.AddSingleton<VatSubmissionTools>();
            services.AddSingleton<VatResources>();
            services.AddSingleton<VatPrompts>();
            services.AddSingleton<McpServer>();

            // Register hosted service
            services.AddHostedService<McpServerHostedService>();
        })
        .Build();

    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Hosted service to run the MCP server
/// </summary>
class McpServerHostedService : IHostedService
{
    private readonly McpServer _server;
    private readonly ILogger<McpServerHostedService> _logger;
    private Task? _executingTask;
    private readonly CancellationTokenSource _stoppingCts = new();

    public McpServerHostedService(McpServer server, ILogger<McpServerHostedService> logger)
    {
        _server = server;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Server Hosted Service is starting");

        _executingTask = _server.RunAsync(_stoppingCts.Token);

        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MCP Server Hosted Service is stopping");

        if (_executingTask == null)
        {
            return;
        }

        try
        {
            await _stoppingCts.CancelAsync();
        }
        finally
        {
            await Task.WhenAny(_executingTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        }
    }
}
