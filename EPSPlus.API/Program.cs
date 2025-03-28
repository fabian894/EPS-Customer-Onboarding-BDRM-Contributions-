using EPSPlus.API.Controllers;
using EPSPlus.Application.Interfaces;
using EPSPlus.Application.Models;
using EPSPlus.Application.Services;
using EPSPlus.Infrastructure;
using EPSPlus.Infrastructure.Repositories;
using Hangfire;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddControllers().AddApplicationPart(typeof(MemberController).Assembly);

// Configure Hangfire
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); // Use Serilog as the logging provider


// Register Infrastructure Layer (DB + Repository)
builder.Services.AddInfrastructure(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EPSPlus API",
        Version = "v1",
        Description = "API documentation for EPS+ Pension Contribution Management System"
    });
});

builder.Services.AddMemoryCache();

// Load Redis configuration
var redisConfig = builder.Configuration.GetSection("Redis:ConnectionString").Value;

// Register Redis connection
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));

var app = builder.Build();

app.Lifetime.ApplicationStopped.Register(() => Log.CloseAndFlush());

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EPSPlus API v1");
        c.RoutePrefix = string.Empty; // Makes Swagger the default landing page
    });
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

// Enable Hangfire Dashboard
app.UseHangfireDashboard();

// Schedule background jobs using Hangfire
RecurringJob.AddOrUpdate<ContributionJobService>(
    "validate-contributions",
    job => job.ValidateContributions(),
    Cron.Daily);

RecurringJob.AddOrUpdate<ContributionJobService>(
    "retry-failed-contributions",
    job => job.RetryFailedContributions(),
    Cron.Hourly);

RecurringJob.AddOrUpdate<ContributionJobService>(
    "update-benefit-eligibility",
    job => job.UpdateBenefitEligibility(),
    Cron.Weekly);
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
