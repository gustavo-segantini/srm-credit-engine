using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
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

    // ---------- JWT Authentication ----------
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"] ?? "CHANGE-ME-IN-PRODUCTION-min-32-chars-secret";

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("JWT authentication failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ---------- Rate Limiting ----------
    builder.Services.AddRateLimiter(limiter =>
    {
        // Default API policy: 100 requests per minute per IP
        limiter.AddFixedWindowLimiter("api", options =>
        {
            options.PermitLimit = 100;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 5;
        });

        // Stricter for pricing simulations: 30 requests per minute per IP
        limiter.AddFixedWindowLimiter("pricing", options =>
        {
            options.PermitLimit = 30;
            options.Window = TimeSpan.FromMinutes(1);
            options.QueueLimit = 2;
        });

        // Respond with 429 Too Many Requests
        limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        limiter.OnRejected = (ctx, token) =>
        {
            Log.Warning("Rate limit exceeded: {Path} from {IP}",
                ctx.HttpContext.Request.Path,
                ctx.HttpContext.Connection.RemoteIpAddress);
            return ValueTask.CompletedTask;
        };
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
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseHttpMetrics();  // Prometheus metrics

    app.MapControllers().RequireRateLimiting("api");
    app.MapMetrics("/metrics");
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
