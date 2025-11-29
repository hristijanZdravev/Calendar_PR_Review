using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarApp.Tests.HelperMocks
{
    public class FakeCalendarExporter : ICalendarExporter
    {
        public Task ExportAsync(CalendarEvent ev, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}
