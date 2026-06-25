namespace ChronosPointer.ModSupport
{
    internal static class ModSupportManager
    {
#if VALPHA4
        private const long VanillaTicksPerDay = 20000L;
        private const long VanillaTicksPerHour = 833L;
#else
        private const long VanillaTicksPerDay = 60000L;
        private const long VanillaTicksPerHour = 2500L;
#endif

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
