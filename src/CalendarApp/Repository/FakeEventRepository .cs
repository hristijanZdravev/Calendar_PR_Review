namespace CalendarApp.Repository
{
    public interface IEventRepository
    {
        Task<List<CalendarEvent>> GetAllEventsAsync();
        Task<CalendarEvent?> GetEventByIdAsync(string id);
        Task AddEventAsync(CalendarEvent ev);
        Task UpdateEventAsync(string id, CalendarEvent ev);
        Task DeleteEventAsync(string id);
    }

    public class FakeEventRepository : IEventRepository
    {
        private readonly Dictionary<string, CalendarEvent> _events = new();
        private readonly ConsoleLogger _logger;

        public FakeEventRepository(ConsoleLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<List<CalendarEvent>> GetAllEventsAsync()
        {
            _logger.Info($"Retrieving all events. Count: {_events.Count}");
            return Task.FromResult(_events.Values.ToList());
        }

        public Task<CalendarEvent?> GetEventByIdAsync(string id)
        {
            _logger.Info($"Retrieving event with ID: {id}");
            _events.TryGetValue(id, out var ev);
            return Task.FromResult(ev);
        }

        public Task AddEventAsync(CalendarEvent ev)
        {
            var id = Guid.NewGuid().ToString();
            _events[id] = ev;
            _logger.Info($"Added event '{ev.Title}' with ID: {id}");
            return Task.CompletedTask;
        }

        public Task UpdateEventAsync(string id, CalendarEvent ev)
        {
            if (!_events.ContainsKey(id))
            {
                _logger.Error($"Event with ID {id} not found");
                throw new KeyNotFoundException($"Event with ID {id} not found");
            }
            _events[id] = ev;
            _logger.Info($"Updated event with ID: {id}");
            return Task.CompletedTask;
        }

        public Task DeleteEventAsync(string id)
        {
            if (_events.Remove(id))
            {
                _logger.Info($"Deleted event with ID: {id}");
            }
            else
            {
                _logger.Warn($"Event with ID {id} not found for deletion");
            }
            return Task.CompletedTask;
        }
    }
}
