using BuildingBlock.Api;
using BuildingBlock.Api.Logging;
using BuildingBlock.Domain.Results;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddSerilogBootstrap("ITC.Api");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
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
app.Run();