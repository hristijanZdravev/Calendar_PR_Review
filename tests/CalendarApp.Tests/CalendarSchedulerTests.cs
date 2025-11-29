using CalendarApp.Repository;
using CalendarApp.Services;
using CalendarApp.Tests.HelperMocks;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CalendarApp.Tests
{
    public class CalendarSchedulerTests
    {
        private CalendarScheduler CreateScheduler(
            ICalendarExporter? exporter = null,
            IEventRepository? repository = null,
            string maxRetries = "3",
            string expandDays = "7")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["GoogleCalendar:Token"] = "test_token",
                    ["GoogleCalendar:CalendarId"] = "test@calendar.com",
                    ["GoogleCalendar:MaxRetries"] = maxRetries,
                    ["GoogleCalendar:ExpandDays"] = expandDays
                })
                .Build();

            var logger = new ConsoleLogger();
            var expander = new EventExpander();
            var testExporter = exporter ?? new FakeCalendarExporter();
            var testRepository = repository ?? new FakeEventRepository(logger);

            return new CalendarScheduler(config, logger, expander, testExporter, testRepository);
        }

        [Fact]
        public async Task ScheduleAsync_ValidRecurringEvent_ExportsAllOccurrences()
        {
            // Arrange
            var exportCount = 0;
            var mockExporter = new CountingCalendarExporter(() => exportCount++);
            var scheduler = CreateScheduler(exporter: mockExporter, expandDays: "30");

            // Act
            await scheduler.ScheduleAsync(
                title: "Daily Standup",
                start: DateTimeOffset.UtcNow.ToString("o"),
                durationMinutes: "15",
                recurrence: "daily",
                count: "5",
                until: "2025-12-31T15:30:00Z"
            );

            // Assert
            Assert.Equal(5, exportCount);
        }
    }
}
