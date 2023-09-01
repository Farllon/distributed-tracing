using Microsoft.EntityFrameworkCore;

using Blog.Authors.API.Data;
using Blog.Authors.API.Models;
using Blog.Authors.API.Endpoints;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Enrichers.Span;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostContext, services, configuration) =>
    configuration.ReadFrom.Configuration(hostContext.Configuration).Enrich.WithSpan());

builder.Services.AddDbContext<ApplicationContext>(optionsBuilder =>
    optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("AuthorsDb")));

builder.Services.AddCors();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            //.AddSource("AuthorsAPI")
            .ConfigureResource(resource => resource
                .AddService("AuthorsAPI"))
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration.GetValue<string>("Tempo:Url"));
                opt.Protocol = builder.Configuration.GetValue<OpenTelemetry.Exporter.OtlpExportProtocol>("Tempo:ExportProtocol");
            }))
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .ConfigureResource(resource => resource
                .AddService("AuthorsAPI"))
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter());

var app = builder.Build();

app.UseSerilogRequestLogging();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

    ctx.Database.Migrate();
    ctx.Authors.Add(new Author("Farllon"));
    ctx.SaveChanges();
}

app.UseCors(c => c
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.MapAuthors();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();