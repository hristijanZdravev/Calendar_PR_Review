# Hristijan Zdravev PR Review Notes: Calendar Scheduler + Google Calendar

Summary
-------
This PR has added a synchronous class called `CalendarScheduler` which is a class that handels all the logic and is badly designed it has no logging or error handling, hardcoded secrets and all around bad coding practices(bad parsing, bad formatting...) and a fake `Api` controller with no validations, no async and no return of status code(Ok, Bad Request..). There is high security and reliability issues int this PR. The existing `GoogleCalendarExporter` and `EventExpander` are good functionally but additional tests are needed.

Key Risks:
---------
- Secret exposure: hardcoded token in `CalendarScheduler` GoogleToken and hardcoded CalendarId.
- `CalendarScheduler` is God class: handles parsing, recurrence, HTTP calls, storage, and scheduling all in one method.
- Calls in `CalendarScheduler` and `Api` are synchronous this will lead to deadlocks and single thread issues.
- Incorrect date time handling and parsing inside the function `Schedule()`. (DateTime.Parse)
- Public static `eventsList` and `HttpClient` are not thread-safe and hurt testability one is mutable the other one is a global client.
- No input validation or API safety in `Api` `PostSchedule` has no checks or any REST API path.
- Limited logs or retry behavior for network/errors.
- RRULE formatting may be incorrect.

Must-fix:
---------
- Reusability, some of the classes already have what is needed for the implementation.
- Hardcoded token in `CalendarScheduler` GoogleToken. Remove hardcoded token and calendar ID, inject via configuration or secret manager.
- Break up CalendarScheduler into smaller responsibilitie implement interfaces and dependency injection.
- Make function calls in `CalendarScheduler` and `Api` change to async current code can deadlock and block threads.
- Schedule() currently always returns `"ok"`, needs to add error handling and logging in console.
- Remove public statics from `eventsList` and `HttpClient` and make `HttpClient` injected(dependency injection) this will solve the thread-safe and testability issues.
- Fix incorrect datetime format and timezone handling.
- No input validation for `Api` PostSchedule, make Api controller and add route with validations and return of status code.
- Implement SOLID principles

Follow-ups:
---------
- Add robust retry for transient HTTP errors, for `Retry-After`.
- Add metrics and structured logs.
- Add more unit and in the future integration or E2E tests.
- Add handling for duplicate submits (operation id or client-supplied id).
- Add check of secret to CI by scanning.
- Add tests for replayed HTTP fixtures for Google Calendar API.
- Improve decoupling and design principles, such as SOLID. 
- In future iterations, use the Repository and Service patterns to persiste and separate business logic.
- Improve error handling.
- Improve API safety and validation for all endpoints.
- Stray away from classes and functions that do everything.

Safe rollout plan:
---------
1. Block merging until hardcoded tokens are removed and tests pass locally.
2. Implement fixes for blocking I/O and error handling, add unit tests.
3. Observe logs(events, call of functions, return codes from Api...)
3. Add flag for WIP in merge.
4. If exists check CI/CD pipes.
4. If any failures are observed, disable exporter and investigate.

Inline comments and their problems:
---------
- src/CalendarApp/CalendarScheduler.cs:8 → public static List<object> eventsList = new List<object>(); (mutable public state) .
- src/CalendarApp/CalendarScheduler.cs:9 → public static HttpClient http = new HttpClient();  (global public HttpClient instance).
- src/CalendarApp/CalendarScheduler.cs:10 → public static string GoogleToken = "ya29.a0AfH6SMADEUPHARDCODED";  (hardcoded secret in source).
- src/CalendarApp/CalendarScheduler.cs:24 → DateTime s = DateTime.Parse(start);  (parsing without culture/kind loses offset and fails DST/timezones).
- src/CalendarApp/CalendarScheduler.cs:27 → payload["start"] = ... s.ToString("s")  ("s" is not with offset).
- src/CalendarApp/CalendarScheduler.cs:52 → var resp = http.Send(req); var body = resp.Content.ReadAsStringAsync().Result; (Synchronous calls on async API).
- src/CalendarApp/CalendarScheduler.cs:58 → catch (Exception) { }  (log and return a failure).
- src/CalendarApp/Api.cs:11 → return Schedule(body["title"], ...);  (This will throw exception for missing fields).
- src/CalendarApp/GoogleCalendarExporter.cs:31 → using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false); if (!resp.IsSuccessStatusCode) { ... throw new HttpRequestException(...) }  (should be more resilient, add retries).

Tests I added:
---------
- The `CalendarSchedulerTests` class verifies that ScheduleAsync correctly expands recurring events and exports all occurrences using a mock exporter.
- The `ApiTests` class verifies that PostScheduleAsync correctly handles valid input and returns an OkObjectResult.

Tests to add (high priority):
---------
- Integration-style test: capture requests via mock `HttpMessageHandler` to validate retries/headers/idempotency behavior.
- Secret scan test / CI step that fails build if token-looking strings are found.

Proposed remediation path (step-by-step):
---------
*   **Removed hardcoded secrets** – Google token and calendar ID are now injected via configuration.
    
*   **Refactored CalendarScheduler** – split responsibilities: scheduling logic, expansion (EventExpander), exporting (GoogleCalendarExporter), and persistence (IEventRepository).
    
*   **Made all operations async** – eliminated synchronous HTTP calls to prevent deadlocks.

*   **Dependency Injection** – added dependency injection to CalendarScheduler.
    
*   **Replaced static mutable state** – removed eventsList and global HttpClient; now use injected repository and HttpClient.
    
*   **Introduced input validation** – validated title, start, durationMinutes, recurrence, count, until before processing.
    
*   **Added proper logging** – info, warnings, and error messages using ConsoleLogger.
    
*   **Implemented retries for transient HTTP failures** – configurable MaxRetries from configuration with exponential backoff.
    
*   **Used EventExpander for recurrence** – generates occurrences within a configurable window (ExpandDays).
    
*   **Updated API controller** – now ApiController with route, async calls, input validation, and proper HTTP status codes (Ok, BadRequest, 500).
    
*   **Ensured duplicate events are avoided** – repository check prevents adding the same event multiple times.
    
*   **Cleaned up RRULE handling** – recurrence handled properly using ScheduleRule and EventExpander.

*   **Added unit tests** – Added two unit test one for CalendarScheduler other one for Api.
