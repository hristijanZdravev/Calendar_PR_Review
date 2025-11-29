using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarApp.Tests.HelperMocks
{
    public class CountingCalendarExporter : ICalendarExporter
    {
        private readonly Action _onExport;

        public CountingCalendarExporter(Action onExport)
        {
            _onExport = onExport;
        }

        public Task ExportAsync(CalendarEvent ev, CancellationToken ct = default)
        {
            _onExport();
            return Task.CompletedTask;
        }
    }
}
