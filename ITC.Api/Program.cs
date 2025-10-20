using BuildingBlock.Api;
using BuildingBlock.Api.Bootstrap;
using BuildingBlock.Api.Logging;
using BuildingBlock.Domain.Results;
using ITC.Domain.Resources;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSerilogBootstrap("ITC.Api");
builder.Services.AddSharedLocalization(opts =>
{
    opts.SupportedCultures = new[] { "ar", "en" };
    opts.DefaultCulture = "ar";               // لو حاب تخليها "en" غيّرها
    opts.AllowQueryStringLang = true;         // يدعم ?culture=ar أو ?lang=ar
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseSerilogPipeline();
app.UseSharedLocalization();

app.MapLoggingDiagnostics();

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