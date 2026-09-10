using System.Threading.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Timlog.Application.Commands;
using Timlog.Application.Interfaces;
using Timlog.Application.Validators;
using Timlog.Infrastructure.Aade;
using Timlog.Infrastructure.Ai;
using Timlog.Infrastructure.Data;
using Timlog.Infrastructure.Repositories;
using Timlog.Infrastructure.Security;
using Timlog.Infrastructure.Telegram;
using System.Globalization;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("el-GR");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("el-GR");

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/timlog-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    var otlpEndpointUrl = "http://localhost:18890";
    
    // 1. Ρύθμιση Traces & Metrics
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("Timlog.Api"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts => 
            {
                opts.Endpoint = new Uri($"{otlpEndpointUrl}/v1/traces");
                opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(opts => 
            {
                opts.Endpoint = new Uri($"{otlpEndpointUrl}/v1/metrics");
                opts.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
            }));

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/timlog-.log", rollingInterval: RollingInterval.Day)
        .WriteTo.OpenTelemetry(opts => 
        {
            opts.Endpoint = $"{otlpEndpointUrl}/v1/logs";
            opts.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.HttpProtobuf;
        }));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 50,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    builder.Services.AddDbContext<TimlogDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<TimlogDbContext>();

    builder.Services.AddHttpClient<IInvoiceTextParser, OllamaInvoiceParser>()
        .AddStandardResilienceHandler();
    
    builder.Services.AddHttpClient<IInvoiceTextParser, GeminiInvoiceParser>()
        .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<ITelegramClient, TelegramClient>(client =>
        {
            var botToken = builder.Configuration["Telegram:BotToken"] ?? throw new InvalidOperationException("Telegram:BotToken is missing.");
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{botToken}/");
        })
        .AddStandardResilienceHandler();

    builder.Services.AddHttpClient<IAadeClient, AadeClient>(client =>
        {
            client.BaseAddress = new Uri(builder.Configuration["Aade:BaseUrl"] ?? throw new InvalidOperationException("Aade:BaseUrl is missing."));
        })
        .AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
            options.Retry.MaxRetryAttempts = 4;
            options.Retry.Delay = TimeSpan.FromSeconds(3);
        });

    builder.Services.AddDataProtection();
    builder.Services.AddValidatorsFromAssemblyContaining<SetupCredentialsCommandValidator>();
    builder.Services.AddSingleton<ISetupStateService, SetupStateService>();
    builder.Services.AddScoped<SetupFlowHandler>();
    builder.Services.AddScoped<ApproveInvoiceCommandHandler>();
    builder.Services.AddScoped<SetupCredentialsCommandHandler>();
    builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
    builder.Services.AddScoped<ICredentialRepository, CredentialRepository>();
    builder.Services.AddScoped<CreateInvoiceFromTextCommandHandler>();
    builder.Services.AddScoped<ICredentialEncryptionService, CredentialEncryptionService>();

    var app = builder.Build();

    app.UseForwardedHeaders();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHealthChecks("/health").DisableRateLimiting();

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