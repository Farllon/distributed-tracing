using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Blog.Authors.gRPC.Data;
using Blog.Authors.gRPC.Services;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostContext, services, configuration) =>
    configuration.ReadFrom.Configuration(hostContext.Configuration).Enrich.WithSpan());

builder.WebHost.ConfigureKestrel(options =>
{
    // Setup a HTTP/2 endpoint without TLS.
    options.ListenAnyIP(8000, o => o.Protocols =
        HttpProtocols.Http2);
});

builder.Services.AddDbContext<ApplicationContext>(optionsBuilder =>
    optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("AuthorsDb")));

builder.Services.AddGrpc();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            //.AddSource("AuthorsGRPC")
            .ConfigureResource(resource => resource
                .AddService("AuthorsGRPC"))
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(opt => opt.SetDbStatementForText = true)
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration.GetValue<string>("Tempo:Url"));
                opt.Protocol = builder.Configuration.GetValue<OpenTelemetry.Exporter.OtlpExportProtocol>("Tempo:ExportProtocol");
            }))
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .ConfigureResource(resource => resource
                .AddService("AuthorsGRPC"))
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter());

var app = builder.Build();

app.UseSerilogRequestLogging();

app.MapGrpcService<AuthorsService>();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
