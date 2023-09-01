using Authors;
using MongoDB.Driver;

using Blog.Posts.API.Data;
using Blog.Posts.API.Endpoints;
using Blog.Posts.API;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using static Blog.Posts.API.Rabbit;
using Serilog;
using Serilog.Enrichers.Span;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((hostContext, services, configuration) =>
    configuration.ReadFrom.Configuration(hostContext.Configuration).Enrich.WithSpan());

builder.Services.AddScoped(provider =>
{
    var configurations = provider.GetRequiredService<IConfiguration>();
    var mongoSection = configurations.GetSection("MongoDb");

    var settings = MongoClientSettings.FromConnectionString(mongoSection["ConnectionString"]);

    settings.ClusterConfigurator = (clusterBuilder) => clusterBuilder.Subscribe(new DiagnosticsActivityEventSubscriber());

    var client = new MongoClient(settings);

    var database = client.GetDatabase(mongoSection["Database"]);

    return new ApplicationContext(database);
});

builder.Services.AddRabbit(builder.Configuration);

builder.Services
    .AddGrpcClient<AuthorsRpc.AuthorsRpcClient>(options =>
        options.Address = new Uri(builder.Configuration.GetValue<string>("AuthorsRPC")));

builder.Services.AddCors();

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(nameof(IRabbitManager))
            .ConfigureResource(resource => resource
                .AddService("PostsAPI"))
            .AddAspNetCoreInstrumentation()
            .AddGrpcClientInstrumentation(opt => opt.SuppressDownstreamInstrumentation = true)
            .AddHttpClientInstrumentation()
            .AddMongoDBInstrumentation()
            .AddOtlpExporter(opt =>
            {
                opt.Endpoint = new Uri(builder.Configuration.GetValue<string>("Tempo:Url"));
                opt.Protocol = builder.Configuration.GetValue<OpenTelemetry.Exporter.OtlpExportProtocol>("Tempo:ExportProtocol");
            }))
    .WithMetrics(metricsProviderBuilder =>
        metricsProviderBuilder
            .ConfigureResource(resource => resource
                .AddService("PostsAPI"))
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter());

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors(c => c
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowAnyOrigin());

app.MapPosts();

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();