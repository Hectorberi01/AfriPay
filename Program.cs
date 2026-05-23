using AfriPay.API.Middleware;
using AfriPay.API.Swagger;
using AfriPay.Application;
using AfriPay.Application.Auth;
using AfriPay.Application.Common.Behaviors;
using AfriPay.Application.Notifications;
using AfriPay.Infrastructure;
using AfriPay.Infrastructure.Jobs;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // SERILOG

    builder.Host.UseSerilog((ctx, services, config) =>
        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "AfriPay.API")
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                "{Properties:j}{NewLine}{Exception}"));

    // SERVICES

    // Application (MediatR + FluentValidation + behaviors)
    builder.Services.AddApplication();

    // Enregistre LoggingBehavior dans le pipeline MediatR
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

    // Infrastructure
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.NotificationExtention(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // ASP.NET Core
    builder.Services.AddControllers();
    builder.Services.AddAfriPaySwagger();
    builder.Services.AddEndpointsApiExplorer();
    
    // Background
    builder.Services.AddAfriPayJobs();
    

    // Rate limiting par plan marchand
    builder.Services.AddRateLimiter(opts =>
    {
        // Plan Starter : 100 req/min
        opts.AddSlidingWindowLimiter("starter", o =>
        {
            o.Window = TimeSpan.FromMinutes(1);
            o.SegmentsPerWindow = 6;
            o.PermitLimit = 100;
            o.QueueLimit = 10;
        });

        // Plan Growth : 1 000 req/min
        opts.AddSlidingWindowLimiter("growth", o =>
        {
            o.Window = TimeSpan.FromMinutes(1);
            o.SegmentsPerWindow = 6;
            o.PermitLimit = 1_000;
            o.QueueLimit = 50;
        });

        // Plan Scale : pas de limite (géré par Traefik en amont)
        opts.RejectionStatusCode = 429;
    });

    // Health checks
    builder.Configuration.AddEnvironmentVariables();
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("AfriPayLive")!,
            name: "postgres-live",
            tags: ["db", "live"])
        .AddNpgSql(
            builder.Configuration.GetConnectionString("AfriPaySandbox")!,
            name: "postgres-sandbox",
            tags: ["db", "sandbox"])
        .AddRedis(
            builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379",
            name: "redis",
            tags: ["cache"]);
    
    var app = builder.Build();

    if (!builder.Environment.IsEnvironment("EF"))
    {
        await app.Services.InitializeDatabaseAsync();
    }

    if (args.Contains("migrate"))
    {
        Log.Information("Migration mode — exiting.");
        return 0;
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0}ms";
    });

    app.UseAuthentication();
    app.UseAuthorization();

    // Documentation API (Scalar) — exemptée d'auth
    app.UseAfriPayScalar();

    app.MapHealthChecks("/health");

    app.UseMiddleware<ApiKeyAuthMiddleware>();
    app.UseMiddleware<IdempotencyMiddleware>();
    app.UseRateLimiter();
    app.MapControllers();

    Log.Information("AfriPay.API started on {Urls}", app.Urls);
    Log.Information("Documentation disponible sur http://localhost:5030/docs");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AfriPay.API failed to start.");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;