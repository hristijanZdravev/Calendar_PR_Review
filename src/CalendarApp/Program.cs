using CalendarApp;
using CalendarApp.Repository;
using CalendarApp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);


// Add HttpClient
builder.Services.AddHttpClient();

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConsoleLogger>();
builder.Services.AddSingleton<IEventExpander, EventExpander>();
builder.Services.AddScoped<IEventRepository>(sp =>
{
    var logger = sp.GetRequiredService<ConsoleLogger>();
    return new FakeEventRepository(logger);
});
builder.Services.AddScoped<CalendarScheduler>();

// Register the exporter with the token from config
builder.Services.AddScoped<ICalendarExporter>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ConsoleLogger>();

    var token = config["GoogleCalendar:Token"]
       ?? throw new InvalidOperationException("GoogleCalendar:Token is missing from configuration");
    var calendarId = config["GoogleCalendar:CalendarId"]
        ?? throw new InvalidOperationException("GoogleCalendar:CalendarId is missing from configuration");

    return new GoogleCalendarExporter(httpClient, token, calendarId, logger);
});

// Add controller support
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Calendar API", Version = "v1" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Calendar API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();

app.Run();
