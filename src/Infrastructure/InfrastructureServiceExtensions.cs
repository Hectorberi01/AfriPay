using AfriPay.Infrastructure.Caching;
using AfriPay.Domain.Repositories;
using AfriPay.Infrastructure.Persistence;
using AfriPay.Infrastructure.Providers;
using AfriPay.Infrastructure.Providers.Abstractions;
using AfriPay.Infrastructure.Providers.MtnMomo;
using AfriPay.Infrastructure.Providers.Paypal;
using AfriPay.Infrastructure.Providers.Stripe;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;

namespace AfriPay.Infrastructure;

/// <summary>
/// Point d'entrée unique pour enregistrer toute la couche Infrastructure.
///
/// Usage dans Program.cs :
/// <code>
/// builder.Services.AddInfrastructure(builder.Configuration);
/// </code>
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
    {
        services
            .AddDatabase(configuration)
            .AddRepositories()
            .AddProviders(configuration)
            .AddCaching(configuration);

        return services;
    }

    // BASE DE DONNÉES

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var liveConn    = configuration.GetConnectionString("AfriPayLive")
                          ?? throw new InvalidOperationException("Missing 'AfriPayLive' connection string.");

        var sandboxConn = configuration.GetConnectionString("AfriPaySandbox")
                          ?? throw new InvalidOperationException("Missing 'AfriPaySandbox' connection string.");
        
        var sensitive = configuration.GetValue<bool>("Database:EnableSensitiveLogging");
        
        var migrationsAssembly = typeof(LiveDbContext).Assembly.GetName().Name;

        services.AddDbContext<LiveDbContext>(o =>
        {
            o.UseNpgsql(liveConn, n =>
            {
                n.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                n.CommandTimeout(30);
                n.MigrationsHistoryTable("__ef_migrations");
                n.MigrationsAssembly(migrationsAssembly);
            });
            o.EnableSensitiveDataLogging(sensitive);
        });

        services.AddDbContext<SandboxDbContext>(o =>
        {
            o.UseNpgsql(sandboxConn, n =>
            {
                n.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                n.CommandTimeout(30);
                n.MigrationsHistoryTable("__ef_migrations");
                n.MigrationsAssembly(migrationsAssembly);
                n.MigrationsRootDirectory("Migrations/SandboxDb");
            });
            o.EnableSensitiveDataLogging(sensitive);
        });

        services.AddScoped<IDbContextFactory, AfriPayDbContextFactory>();
        services.AddHttpContextAccessor();
        services.AddScoped<IRequestEnvironment, HttpRequestEnvironment>();
        
        return services;
    }

    // REPOSITORIES & UNIT OF WORK
    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    // PROVIDERS DE PAIEMENT
    private static IServiceCollection AddProviders(this IServiceCollection services, IConfiguration configuration)
    {
        // Orchestrateur — implémente IPaymentOrchestrator (défini dans Application)
        services.AddScoped<IPaymentOrchestrator, PaymentOrchestratorImpl>();

        // Factory de providers (Singleton — stateless)
        // Scoped : les adapters IPaymentProvider sont Scoped — la factory doit l'être aussi.
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();

        // Adapters

        // MTN MoMo (Phase 1)
        services.AddMtnMomoAdapter(configuration);
        
        services.AddPayPalAdapter(configuration);

        // Wave (Phase 2 — décommenter quand implémenté)
        // services.AddWaveAdapter(configuration);

        // Moov Money (Phase 2)
        // services.AddMoovAdapter(configuration);

        // Stripe
        services.AddStripeAdapter(configuration);

        // Polly — pipelines de résilience par provider
        // Deux profils : Mobile Money (instable, 3 retries) et Carte (fiable, 2 retries)

        services.AddResiliencePipeline("mtn_momo",    BuildMobileMoneyPipeline);
        services.AddResiliencePipeline("moov_money",  BuildMobileMoneyPipeline);
        services.AddResiliencePipeline("wave",         BuildMobileMoneyPipeline);
        services.AddResiliencePipeline("orange_money", BuildMobileMoneyPipeline);
        services.AddResiliencePipeline("stripe",       BuildCardPipeline);
        services.AddResiliencePipeline("paypal",       BuildCardPipeline);

        return services;
    }

    // MTN MoMo adapter

    private static IServiceCollection AddMtnMomoAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MtnMomoConfig>(configuration.GetSection(MtnMomoConfig.SectionName));

        services.AddHttpClient<MtnMomoClient>((sp, http) =>
        {
            var config = configuration
                .GetSection(MtnMomoConfig.SectionName)
                .Get<MtnMomoConfig>()!;

            http.BaseAddress = new Uri(config.BaseUrl);
            http.Timeout     = TimeSpan.FromSeconds(config.TimeoutSeconds);
        });

        // Enregistré comme type concret ET comme IPaymentProvider
        // pour que la factory puisse résoudre IEnumerable<IPaymentProvider>
        // Transient : les adapters sont stateless (le token est dans IMemoryCache).
        // Transient est compatible avec la factory Singleton via IServiceProvider.
        services.AddTransient<MtnMomoAdapter>();
        services.AddTransient<IPaymentProvider>(
            sp => sp.GetRequiredService<MtnMomoAdapter>());

        return services;
    }
    
    private static IServiceCollection AddPayPalAdapter(this IServiceCollection services,IConfiguration configuration)
    {
        services.Configure<PayPalConfig>(
            configuration.GetSection(PayPalConfig.SectionName));
 
        services.AddHttpClient<PayPalClient>((sp, http) =>
        {
            http.Timeout = TimeSpan.FromSeconds(30);
        });
 
        services.AddTransient<PayPalAdapter>();
        services.AddTransient<IPaymentProvider>(
            sp => sp.GetRequiredService<PayPalAdapter>());
 
        return services;
    }

    // Stripe adapter

    private static IServiceCollection AddStripeAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StripeConfig>(configuration.GetSection(StripeConfig.SectionName));

        services.AddHttpClient<StripeAdapter>((sp, http) =>
        {
            http.BaseAddress = new Uri("https://api.stripe.com/");
            http.Timeout     = TimeSpan.FromSeconds(30);
        });

        services.AddTransient<StripeAdapter>();
        services.AddTransient<IPaymentProvider>(
            sp => sp.GetRequiredService<StripeAdapter>());

        return services;
    }

    // Polly — Mobile Money pipeline 
    // 3 retries exponentiels + circuit breaker : ouvre après 5 échecs en 60s

    private static void BuildMobileMoneyPipeline(ResiliencePipelineBuilder builder)
    {
        builder.AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay            = TimeSpan.FromSeconds(1),
            BackoffType      = DelayBackoffType.Exponential,
            UseJitter        = true,
            ShouldHandle     = static args => args.Outcome switch
            {
                { Exception: HttpRequestException ex }
                    when ex.StatusCode is
                        System.Net.HttpStatusCode.ServiceUnavailable or
                        System.Net.HttpStatusCode.GatewayTimeout     or
                        System.Net.HttpStatusCode.TooManyRequests
                    => PredicateResult.True(),
                _ => PredicateResult.False(),
            },
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.5,
            MinimumThroughput = 5,
            SamplingDuration  = TimeSpan.FromSeconds(60),
            BreakDuration     = TimeSpan.FromSeconds(30),
            OnOpened          = static args =>
            {
                return ValueTask.CompletedTask;
            },
        });
    }

    // Polly — Carte / Wallet pipeline
    // Stripe et PayPal sont plus fiables → 2 retries, seuils plus stricts

    private static void BuildCardPipeline(ResiliencePipelineBuilder builder)
    {
        builder.AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay            = TimeSpan.FromMilliseconds(500),
            BackoffType      = DelayBackoffType.Exponential,
            UseJitter        = true,
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio      = 0.4,
            MinimumThroughput = 10,
            SamplingDuration  = TimeSpan.FromSeconds(30),
            BreakDuration     = TimeSpan.FromSeconds(60),
        });
    }

    // CACHE (Redis ou mémoire en fallback)
    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName  = "afripay:";
            });
        }
        else
        {
            // Fallback mémoire : développement local ou tests
            services.AddDistributedMemoryCache();
        }

        // IMemoryCache pour le cache de tokens OAuth2 MTN MoMo (in-process)
        services.AddMemoryCache();

        // Service de cache unifié — wrapping IDistributedCache + IMemoryCache
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}

// DATABASE INITIALIZER

/// <summary>
/// Applique les migrations EF Core au démarrage de l'application.
///
/// Usage dans Program.cs après app.Build() :
/// <code>
/// await app.Services.InitializeDatabaseAsync();
/// </code>
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope  = services.CreateAsyncScope();
        var sp     = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<LiveDbContext>>();

        try
        {
            var live    = sp.GetRequiredService<LiveDbContext>();
            var sandbox = sp.GetRequiredService<SandboxDbContext>();

            // En développement sans migrations — créer le schéma directement
            var env = sp.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                logger.LogInformation("Dev mode — EnsureCreated afripay_live…");
                await live.Database.EnsureCreatedAsync();

                logger.LogInformation("Dev mode — EnsureCreated afripay_sandbox…");
                await sandbox.Database.EnsureCreatedAsync();
            }
            else
            {
                logger.LogInformation("Applying migrations → afripay_live…");
                await live.Database.MigrateAsync();

                logger.LogInformation("Applying migrations → afripay_sandbox…");
                await sandbox.Database.MigrateAsync();
            }

            logger.LogInformation("Both databases ready.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Database migration failed.");
            throw;
        }
    }
}