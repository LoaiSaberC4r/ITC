using BuildingBlock.Api.Middleware;
using BuildingBlock.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace BuildingBlock.Api.Logging
{
    public static class SerilogBootstrapper
    {
        private static readonly LoggingLevelSwitch GlobalLevel = new(LogEventLevel.Information);

        /// <summary>
        /// يفعّل Serilog موحّد ويقرأ إعداداته من appsettings الخاصة بالخدمة المستهلكة.
        /// </summary>
        public static WebApplicationBuilder AddSerilogBootstrap(this WebApplicationBuilder builder, string domainArea)
        {
            // SelfLog: STDERR + ملف (best effort)
            Directory.CreateDirectory("logs");
            SelfLog.Enable(msg =>
            {
                try
                {
                    var line = $"[{DateTime.UtcNow:O}] {msg}";
                    Console.Error.WriteLine("SERILOG-SELFLOG: " + line);
                    File.AppendAllText(Path.Combine("logs", "serilog-selflog.txt"), line + Environment.NewLine);
                }
                catch { /* ignore */ }
            });

            // Bind options (Sampling/Durable ..)
            var opt = new LoggingOptions();
            builder.Configuration.GetSection("Logging").Bind(opt);

            // Base config from appsettings + common enrichers
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .MinimumLevel.ControlledBy(GlobalLevel)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.With(new DomainAreaEnricher(domainArea))
                .Enrich.With(new RedactionEnricher());

            if (opt.Sampling?.Enabled == true && opt.Sampling.KeepRate is > 0 and < 1)
            {
                var maxLvl = ParseLevelOrDefault(opt.Sampling.MaxLevelToSample, LogEventLevel.Information);
                loggerConfig = loggerConfig.Filter.With(new SamplingFilter(opt.Sampling.KeepRate, maxLvl));
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();

            // سجّل الـ switches للاستهلاك (اختياري)
            builder.Services.AddSingleton(GlobalLevel);

            // Middlewares & endpoints dependencies
            builder.Services.AddHttpContextAccessor();

            return builder;
        }

        /// <summary>
        /// يشغّل الـ middlewares القياسية + ASP.NET request logging.
        /// </summary>
        public static IApplicationBuilder UseSerilogPipeline(this IApplicationBuilder app)
        {
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Request logging
            app.UseSerilogRequestLogging(opts =>
            {
                opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms (corr={CorrelationId})";
                opts.EnrichDiagnosticContext = (diag, http) =>
                {
                    diag.Set("RequestPath", http.Request.Path);
                    diag.Set("RequestMethod", http.Request.Method);
                    diag.Set("ClientIP", http.Connection.RemoteIpAddress?.ToString());
                    diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
                    if (http.Items.TryGetValue(CorrelationIdMiddleware.CorrelationItemKey, out var corr))
                        diag.Set("CorrelationId", corr?.ToString());
                };
            });

            return app;
        }

        /// <summary>
        /// نقاط تشخيص داخلية بسيطة (اختيارية).
        /// </summary>
        public static IEndpointRouteBuilder MapLoggingDiagnostics(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/internal/logging/diag", () =>
            {
                var durablePath = Path.GetFullPath("logs/seq-buffer");
                var failoverPath = Path.GetFullPath("logs");
                return Results.Ok(new
                {
                    durableBufferPath = durablePath,
                    failoverPath,
                    selfLog = Path.Combine(failoverPath, "serilog-selflog.txt"),
                    slo = new { latency99_ms = 1000, maxQueue = 5000 }
                });
            });

            endpoints.MapPost("/internal/logging/level", (string level) =>
            {
                if (Enum.TryParse<LogEventLevel>(level, true, out var parsed))
                {
                    GlobalLevel.MinimumLevel = parsed;
                    Log.Warning("Logging level switched to {Level}", parsed);
                    return Results.Ok(new { level = parsed.ToString() });
                }
                return Results.BadRequest(new { error = "Invalid level. Use: Verbose|Debug|Information|Warning|Error|Fatal" });
            });

            return endpoints;
        }

        private static LogEventLevel ParseLevelOrDefault(string? level, LogEventLevel @default)
            => Enum.TryParse<LogEventLevel>(level, true, out var parsed) ? parsed : @default;
    }
}