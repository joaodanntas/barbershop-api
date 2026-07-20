namespace BarberShopApi.Helpers
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo BrasiliaTz =
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        public static DateTime AgoraBrasil()
        {
            var utcAgora = DateTime.UtcNow;
            var brasiliaAgora = TimeZoneInfo.ConvertTimeFromUtc(utcAgora, BrasiliaTz);
            return DateTime.SpecifyKind(brasiliaAgora, DateTimeKind.Unspecified);
        }
    }
}
