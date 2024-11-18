// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = Host.CreateApplicationBuilder(args);

ConfigureOpenTelemetry(builder);
builder.Services.AddHostedService<BackgroundWorker>();
var host = builder.Build();
host.Run();


static IHostApplicationBuilder ConfigureOpenTelemetry(IHostApplicationBuilder builder)
{
    builder.Logging.AddOpenTelemetry(logging =>
    {
        logging.IncludeFormattedMessage = true;
        logging.IncludeScopes = true;
    });

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(c => c.AddService("AspireDemo.Console"))
        .WithMetrics(metrics =>
        {
            metrics.AddHttpClientInstrumentation()
                   .AddRuntimeInstrumentation();
        })
        .WithTracing(tracing =>
        {
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource("AspireDemo.Console");
        });

    // Use the OTLP exporter if the endpoint is configured.
    builder.Services.Configure<OtlpExporterOptions>(p => p.Headers = $"x-otlp-api-key={{81e0f908-ed22-4d7b-a82f-05b54f95e996}}");
    builder.Services.AddOpenTelemetry().UseOtlpExporter();

    return builder;
}
