namespace CalendarApp.Interfaces
{
    public interface ICalendarScheduler
    {
        Task<string> ScheduleAsync(string title, string start, string durationMinutes,
                string recurrence, string count, string until, CancellationToken ct = default);
    }
}
