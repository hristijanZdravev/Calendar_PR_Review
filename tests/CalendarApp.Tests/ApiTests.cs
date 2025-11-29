using CalendarApp.Controller;
using CalendarApp.Repository;
using CalendarApp.Services;
using CalendarApp.Tests.HelperMocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CalendarApp.Tests
{
    public class ApiTests
    {
        private Api CreateApi(ICalendarExporter? exporter = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GoogleCalendar:Token"] = "test_token",
                    ["GoogleCalendar:CalendarId"] = "test@calendar.com",
                    ["GoogleCalendar:MaxRetries"] = "3",
                    ["GoogleCalendar:ExpandDays"] = "7"
                })
                .Build();

            var logger = new ConsoleLogger();
            var expander = new EventExpander();
            var repo = new FakeEventRepository(logger);
            var testExporter = exporter ?? new FakeCalendarExporter();

            var scheduler = new CalendarScheduler(config, logger, expander, testExporter, repo);
            return new Api(scheduler);
        }

        [Fact]
        public async Task PostScheduleAsync_ValidRequest_ReturnsOk()
        {
            // Arrange
            var api = CreateApi();
            var body = new Dictionary<string, string>
            {
                ["title"] = "Team Meeting",
                ["start"] = "2025-12-01T10:00:00Z",
                ["durationMinutes"] = "60",
                ["recurrence"] = "weekly",
                ["count"] = "5",
                ["until"] = "2025-12-31T23:59:59Z"
            };

            // Act
            var result = await api.PostScheduleAsync(body);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
