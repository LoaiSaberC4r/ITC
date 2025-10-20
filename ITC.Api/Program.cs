using BuildingBlock.Api;
using BuildingBlock.Api.Bootstrap;
using BuildingBlock.Api.Logging;
using BuildingBlock.Domain.Results;
using ITC.Domain.Resources;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ---------Serilog-------- /
builder.AddSerilogBootstrap("ITC.Api");
//---------Localization-------- /
builder.Services.AddSharedLocalization(opts =>
{
    opts.SupportedCultures = new[] { "ar", "en" };
    opts.DefaultCulture = "en";
    opts.AllowQueryStringLang = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Custom Middleware
app.UseSerilogPipeline();
app.UseSharedLocalization();
app.MapLoggingDiagnostics();
///////////////////////
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/log/test", (HttpContext http) =>
{
    Log.Information("🚀 Test log from /log/test at {Time}", DateTime.UtcNow);
    Log.Debug("Debug message - invisible unless level <= Debug");
    var res = Result.Ok();
    return res.ToIResult(http);
});

app.MapGet("/demo/throw", () =>
{
    Log.Information("About to throw an intentional exception for demo");
    throw new InvalidOperationException("Forced demo exception to test middleware.");
});

// 2) يرجّع Result Failure → ProblemDetails (مثلاً Validation=422)
app.MapGet("/demo/result-fail", (HttpContext http) =>
{
    var errors = new[]
    {
        Error.Validation(code: Message.NotFount, message: "Name is required."),
        Error.Validation(code: Message.NotFount,  message: "Age must be >= 18.")
    };
    return Result.Fail(errors).ToIResult(http);
});
app.Run();