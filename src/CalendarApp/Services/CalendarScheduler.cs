using CalendarApp.Interfaces;
using CalendarApp.Repository;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.WebRequestMethods;

namespace CalendarApp.Services
{
    public class CalendarScheduler : ICalendarScheduler 
    {

        public readonly string _maxRetries;
        public readonly string _expandDays;
        private readonly ConsoleLogger _logger;
        private readonly IEventExpander _expander;
        private readonly ICalendarExporter _exporter;

        //Fake Repo
        private readonly IEventRepository _repository;
        public CalendarScheduler(IConfiguration config, ConsoleLogger logger, IEventExpander expander,
            ICalendarExporter exporter, IEventRepository repository)
        {
            _maxRetries = config["GoogleCalendar:MaxRetries"]
                          ?? throw new ArgumentNullException("GoogleCalendar:MaxRetries missing");
            _expandDays = config["GoogleCalendar:ExpandDays"]
                          ?? throw new ArgumentNullException("GoogleCalendar:ExpandDays missing");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _expander = expander ?? throw new ArgumentNullException(nameof(expander));
            _exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        //public static List<object> eventsList = new List<object>(); // shared mutable state
       
        public async Task<string> ScheduleAsync(string title, string start, 
            string durationMinutes, string recurrence, 
            string count, string until, CancellationToken ct = default)
        {
            _logger.Warn($"Cancelation Token is set to default if not explicitly set to other, currently ct: ${ct}");

            if (!DateTimeOffset.TryParse(start, out var startDateTime))
            {
                _logger.Error($"Invalid start datetime: {start}");
                throw new ArgumentException("Invalid start datetime.");
            }

            if (!int.TryParse(durationMinutes, out var duration))
            {
                _logger.Error($"Invalid durationMinutes: {durationMinutes}");
                throw new ArgumentException("Invalid durationMinutes.");
            }

            if (!Enum.TryParse(recurrence, true, out RecurrenceKind recurrenceKind))
            {
                _logger.Error($"Invalid recurrence: {recurrence}");
                throw new ArgumentException("Invalid recurrence.");
            }

            _logger.Info($"Creating Schedule Rule");
            var rule = new ScheduleRule(
                recurrenceKind,
                Interval: 1,
                Count: string.IsNullOrEmpty(count) ? null : int.Parse(count),
                Until: string.IsNullOrEmpty(until) ? null : DateTimeOffset.Parse(until)
            );
            _logger.Info($"Created Schedule Rule: recurrenceKind: ${recurrenceKind}, count: ${count} and until: ${until}");

            _logger.Info("Creating Calendar Event");
            var ev = new CalendarEvent(
                Title: title,
                Start: startDateTime,
                Duration: TimeSpan.FromMinutes(duration),
                Rule: rule
            );
            _logger.Info($"Created Calendar Event: '{title}' at {startDateTime} for {duration} minutes, rule: {rule.ToString()}");

            // Store the event in repository
            var existingEvents = await _repository.GetAllEventsAsync();
            if (existingEvents.Any(e => e.Title == title && e.Start == startDateTime))
            {
                _logger.Warn($"Event '{title}' at {startDateTime} already exists in repository. Skipping add.");
            }
            else
            {
                await _repository.AddEventAsync(ev);
                _logger.Info($"Stored base event '{title}' in repository");
            }

            var windowStart = DateTimeOffset.UtcNow;

            // 7 days default
            int expandDays = 7;
            if (int.TryParse(_expandDays, out var configuredExpandDays))
            {
                expandDays = configuredExpandDays;
            }
            var windowEnd = windowStart.AddDays(expandDays);

            var occurrences = _expander.Expand(ev, windowStart, windowEnd);

            foreach (var (occurrenceStart, occurrenceEnd) in occurrences)
            {
                var occurrenceEvent = ev with { Start = occurrenceStart };

                // Store occurrence as pending
                await _repository.AddEventAsync(occurrenceEvent);

                int maxRetries = 3;
                if (int.TryParse(_maxRetries, out var configuredRetries))
                {
                    maxRetries = configuredRetries;
                }

                int attempt = 0;
                while (true)
                {
                    attempt++;
                    try
                    {
                        await _exporter.ExportAsync(occurrenceEvent, ct).ConfigureAwait(false);
                        _logger.Info($"Exported occurrence at {occurrenceStart}");

                        // update event in repository
                        //await _repository.UpdateEventAsync(/* assume ID mapping */, occurrenceEvent);
                        break;
                    }
                    catch (HttpRequestException ex) when (attempt < maxRetries)
                    {
                        _logger.Warn($"Transient failure exporting occurrence at {occurrenceStart}. Retry {attempt}/{maxRetries}...");
                        await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct);
                    }
                }
            }

            return "Added Schedule";
        }
    }
}
