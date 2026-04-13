using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MyApp.Application.Common.Interfaces;
using MyApp.Domain.Interfaces;
using MyApp.Infrastructure.BackgroundJobs;
using MyApp.Infrastructure.Caching;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.Data.Repositories;
using MyApp.Infrastructure.Identity;
using MyApp.Infrastructure.Messaging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text;

namespace MyApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database (multi-provider) ────────────────────────────────────────
        var dbProvider = configuration["DatabaseProvider"] ?? "SqlServer";
        services.AddDbContext<AppDbContext>(options =>
        {
            if (dbProvider == "PostgreSQL")
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            else
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        // ── Repositories / UoW ──────────────────────────────────────────────
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── JWT Auth ────────────────────────────────────────────────────────
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtService, JwtService>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSection["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        // Allow SignalR to pass token via query string
                        var accessToken = ctx.Request.Query["access_token"];
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        // ── Password Hasher ──────────────────────────────────────────────────
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // ── Redis Cache ──────────────────────────────────────────────────────
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // ── RabbitMQ ─────────────────────────────────────────────────────────
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();

        // ── Hangfire ─────────────────────────────────────────────────────────
        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings();

            if (dbProvider == "PostgreSQL")
                cfg.UsePostgreSqlStorage(c =>
                    c.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection")));
            else
                cfg.UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"));
        });
        services.AddHangfireServer();
        services.AddTransient<SampleRecurringJob>();

        // ── OpenTelemetry ─────────────────────────────────────────────────────
        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                configuration["OpenTelemetry:ServiceName"] ?? "MyApp.API"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddOtlpExporter(o =>
                    o.Endpoint = new Uri(configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317")))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddPrometheusExporter());

        return services;
    }
}
