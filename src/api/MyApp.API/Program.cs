using Hangfire;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MyApp.Application;
using MyApp.Infrastructure;
using MyApp.Infrastructure.BackgroundJobs;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Serilog:SeqUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Application & Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<MyApp.Application.Common.Interfaces.INotificationService,
    MyApp.API.Services.SignalRNotificationService>();

// ── Controllers & OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MyApp API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

// ── SignalR ───────────────────────────────────────────────────────────────────
var redisConn = builder.Configuration.GetConnectionString("Redis");
var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(redisConn))
    signalRBuilder.AddStackExchangeRedis(redisConn);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["*"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ── Health Checks ─────────────────────────────────────────────────────────────
var hcBuilder = builder.Services.AddHealthChecks();
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
if (dbProvider == "PostgreSQL")
    hcBuilder.AddNpgSql(connStr!, name: "postgresql");
else
    hcBuilder.AddSqlServer(connStr!, name: "sqlserver");

hcBuilder
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "redis")
    .AddRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672/", name: "rabbitmq");

// ────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<MyApp.API.Middleware.GlobalExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ── SignalR Hubs ──────────────────────────────────────────────────────────────
app.MapHub<MyApp.API.Hubs.NotificationHub>("/hubs/notifications");

// ── Hangfire Dashboard ────────────────────────────────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new MyApp.API.Filters.HangfireAuthorizationFilter()]
});
JobScheduler.ConfigureRecurringJobs();

// ── Health Checks ─────────────────────────────────────────────────────────────
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// ── Prometheus metrics ────────────────────────────────────────────────────────
app.MapPrometheusScrapingEndpoint("/metrics");

// ── DB Migration (dev only) ───────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<MyApp.Infrastructure.Data.AppDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

public partial class Program { }
