// Fake API controller with no validation, no DI, no async, no status handling
using CalendarApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CalendarApp.Controller
{
    [ApiController]
    [Route("api/schedule")]
    public class Api : ControllerBase
    {
        private readonly CalendarScheduler _scheduler;

        public Api(CalendarScheduler scheduler)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        [HttpPost]
        public async Task<IActionResult> PostScheduleAsync(Dictionary<string, string> body)
        {
             
            if (body == null){
                return BadRequest("Request body is missing.");
            }

            if (!body.TryGetValue("title", out var title) ||
                !body.TryGetValue("start", out var start) ||
                !body.TryGetValue("durationMinutes", out var duration) ||
                !body.TryGetValue("recurrence", out var recurrence))
            {
                return BadRequest("Required fields: title, start, durationMinutes, recurrence.");
            }

            var count = body.TryGetValue("count", out var c) ? c : null;
            var until = body.TryGetValue("until", out var u) ? u : null;

            if (count == null || until == null) {
                return BadRequest("Required fields: count and until.");
            }

            try
            {
                var result = await _scheduler.ScheduleAsync(
                    title, start, duration, recurrence, count, until);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
