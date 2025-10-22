using BuildingBlock.Api.Bootstrap;
using BuildingBlock.Api.Logging;
using BuildingBlock.Api.OpenAi;
using ITC.Application.Bootstrap;
using ITC.Infrastracture.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
// Add services to the container.
builder.Services.InfrastructureInjection(builder.Configuration);
builder.Services.AddApplicationBootstrap();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ResultPatternOperationFilter>();
});
// ---------Serilog-------- /
builder.AddSerilogBootstrap("ITC.Api");
//---------Localization-------- /
builder.Services.AddSharedLocalization(opts =>
{
    opts.SupportedCultures = new[] { "ar", "en" };
    opts.DefaultCulture = "en";
    opts.AllowQueryStringLang = true;
});
builder.Services.AddMemoryCache();

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
app.Run();