using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using SrmCreditEngine.API.Middleware;
using SrmCreditEngine.Application.Extensions;
using SrmCreditEngine.Infrastructure.Extensions;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SRM Credit Engine API");

    var builder = WebApplication.CreateBuilder(args);

    // ---------- Logging ----------
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "SrmCreditEngine")
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");

        if (!string.IsNullOrEmpty(ctx.Configuration["Seq:ServerUrl"]))
            config.WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"]!);
    });

    // ---------- Application Layers ----------
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ---------- API ----------
    builder.Services.AddControllers()
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            opt.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            opt.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddOpenApi();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Frontend", policy =>
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:5173"])
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // ---------- Health Checks ----------
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ---------- Middleware Pipeline ----------
    app.UseMiddleware<GlobalExceptionMiddleware>();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        opts.GetLevel = (httpContext, elapsed, ex) =>
            ex != null || httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : LogEventLevel.Information;
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(opts =>
        {
            opts.Title = "SRM Credit Engine API";
            opts.Theme = ScalarTheme.DeepSpace;
        });
    }

    app.UseCors("Frontend");
    app.UseHttpMetrics();  // Prometheus metrics
    app.MapControllers();
    app.MapMetrics("/metrics");  // Prometheus scrape endpoint
    app.MapHealthChecks("/health");

    // Auto-migrate database on startup
    await app.Services.ApplyMigrationsAsync();

    Log.Information("SRM Credit Engine API started successfully");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
