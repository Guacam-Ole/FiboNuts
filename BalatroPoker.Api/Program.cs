using BalatroPoker.Api.Services;
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Prometheus;

// Build configuration first to read settings
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

// Get Loki configuration from appsettings
var lokiEndpoint = configuration["Serilog:LokiEndpoint"] ?? "http://localhost:3100";
var jobLabel = configuration["Serilog:Labels:job"] ?? "balatro-poker-api";
var environmentLabel = configuration["Serilog:Labels:environment"] ?? "production";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.GrafanaLoki(lokiEndpoint, 
        labels: new List<LokiLabel> 
        {
            new() { Key = "job", Value = jobLabel },
            new() { Key = "environment", Value = environmentLabel }
        })
    .CreateLogger();

Log.Information("Serilog configured - Loki endpoint: {LokiEndpoint}, Job: {Job}, Environment: {Environment}", 
    lokiEndpoint, jobLabel, environmentLabel);

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add our services as singletons to maintain state in memory
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<MetricsService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseRouting();

// Add Prometheus metrics middleware
app.UseHttpMetrics();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => "OK");

// Prometheus metrics endpoint
app.MapMetrics();

try
{
    Log.Information("Starting Balatro Poker API server");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
