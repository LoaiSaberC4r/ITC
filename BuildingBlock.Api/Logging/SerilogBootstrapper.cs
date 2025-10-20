using BuildingBlock.Api.Middleware;
using BuildingBlock.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace BuildingBlock.Api.Logging
{
    public static class SerilogBootstrapper
    {
        private static readonly LoggingLevelSwitch GlobalLevel = new(LogEventLevel.Information);

        public static WebApplicationBuilder AddSerilogBootstrap(this WebApplicationBuilder builder, string domainArea)
        {
            // 0) SelfLog + ملف
            var contentRoot = builder.Environment.ContentRootPath; // مثال: E:\ITC\ITC.Api
            var logsRoot = Path.Combine(contentRoot, "logs");
            Directory.CreateDirectory(logsRoot);

            SelfLog.Enable(msg =>
            {
                try
                {
                    var line = $"[{DateTime.UtcNow:O}] {msg}";
                    Console.Error.WriteLine("SERILOG-SELFLOG: " + line);
                    File.AppendAllText(Path.Combine(logsRoot, "serilog-selflog.txt"), line + Environment.NewLine);
                }
                catch { /* best-effort */ }
            });

            // 1) Options (Sampling/Durable)
            var opt = new LoggingOptions();
            builder.Configuration.GetSection("Logging").Bind(opt);

            // 2) إنشاء المجلدات من قيم appsettings الفعلية (المهمة للـ Seq Durable + Failover)
            // 2.1 ابحث عن bufferBaseFilename (داخل Async.configure[Seq].Args.bufferBaseFilename)
            TryEnsureDirectoryFromConfigPath(builder.Configuration,
                // ترتيب شائع: Serilog:WriteTo[1]=Async -> configure[1]=Seq -> Args:bufferBaseFilename
                "Serilog:WriteTo:1:Args:configure:1:Args:bufferBaseFilename",
                contentRoot);

            // بدائل محتملة لو ترتيب الـ WriteTo تغيّر (نغطي أشهر احتمالين):
            TryEnsureDirectoryFromConfigPath(builder.Configuration,
                "Serilog:WriteTo:0:Args:configure:1:Args:bufferBaseFilename", contentRoot);

            // 2.2 ابحث عن failover file path (Async.configure[File].Args.path)
            TryEnsureDirectoryFromConfigPath(builder.Configuration,
                "Serilog:WriteTo:1:Args:configure:2:Args:path",
                contentRoot);

            // بديل محتمل:
            TryEnsureDirectoryFromConfigPath(builder.Configuration,
                "Serilog:WriteTo:0:Args:configure:2:Args:path",
                contentRoot);

            // 3) تكوين الـ Logger
            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)            // sinks/filters/enrichers من appsettings
                .MinimumLevel.ControlledBy(GlobalLevel)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.With(new DomainAreaEnricher(domainArea))
                .Enrich.With(new RedactionEnricher());

            // 3.1 Sampling لو مفعّل
            if (opt.Sampling?.Enabled == true && opt.Sampling.KeepRate is > 0 and < 1)
            {
                var maxLvl = ParseLevelOrDefault(opt.Sampling.MaxLevelToSample, LogEventLevel.Information);
                loggerConfig = loggerConfig.Filter.With(new SamplingFilter(opt.Sampling.KeepRate, maxLvl));
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();

            // 4) خدمات مساعدة
            builder.Services.AddSingleton(GlobalLevel);
            builder.Services.AddHttpContextAccessor();

            return builder;
        }

        public static IApplicationBuilder UseSerilogPipeline(this IApplicationBuilder app)
        {
            // Middlewares مشتركة
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            // Request logging
            app.UseSerilogRequestLogging(opts =>
            {
                opts.MessageTemplate =
                    "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms (corr={CorrelationId})";
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

        public static IEndpointRouteBuilder MapLoggingDiagnostics(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/internal/logging/diag", (IHostEnvironment env) =>
            {
                var contentRoot = env.ContentRootPath;
                var durablePath = Path.GetFullPath(Path.Combine(contentRoot, "logs", "seq-buffer"));
                var failoverPath = Path.GetFullPath(Path.Combine(contentRoot, "logs"));
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

        /// <summary>
        /// يقرأ قيمة مسار من config (نسبي أو مطلق)، يحوّله لمسار مطلق من ContentRoot ثم ينشئ المجلد الحاوي.
        /// تدعم مفاتيح مثل: Serilog:WriteTo:1:Args:configure:1:Args:bufferBaseFilename أو :path
        /// </summary>
        private static void TryEnsureDirectoryFromConfigPath(IConfiguration cfg, string key, string contentRoot)
        {
            var value = cfg[key];
            if (string.IsNullOrWhiteSpace(value)) return;

            // لو القيمة ملف (bufferBaseFilename أو path)، ناخد مجلد الحفظ
            var full = Path.GetFullPath(value, contentRoot);
            var dir = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir!);
            }
        }
    }
}