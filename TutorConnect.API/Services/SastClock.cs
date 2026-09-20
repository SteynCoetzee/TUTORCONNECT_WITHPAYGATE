namespace TutorConnect.API.Services
{
    // South Africa does not observe daylight saving time, so SAST is always a fixed
    // UTC+2 offset — no TimeZoneInfo lookup needed (those vary by OS/ID naming and
    // would depend on however a given machine's clock/timezone happens to be set,
    // which is exactly what caused the LogHours "date in the future" bug earlier).
    // Used anywhere a timestamp needs to mean "now, in South Africa" regardless of
    // the server's own clock/timezone — e.g. audit log entries, quiz completion times.
    public static class SastClock
    {
        public static DateTime Now => DateTime.UtcNow.AddHours(2);
    }
}
