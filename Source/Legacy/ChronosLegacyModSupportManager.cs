namespace ChronosPointer.ModSupport
{
    internal static class ModSupportManager
    {
        private const long VanillaTicksPerDay = 60000L;
        private const long VanillaTicksPerHour = 2500L;

        public static long GetTicksPerDay()
        {
            return VanillaTicksPerDay;
        }

        public static long GetTicksPerHour()
        {
            return VanillaTicksPerHour;
        }
    }
}
