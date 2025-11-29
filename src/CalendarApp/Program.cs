using CalendarApp;
using CalendarApp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);


// Add HttpClient
builder.Services.AddHttpClient();

// Register services
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConsoleLogger>();
builder.Services.AddSingleton<IEventExpander, EventExpander>();
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

var app = builder.Build();

app.MapControllers();

app.Run();
