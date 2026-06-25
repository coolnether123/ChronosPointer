using Verse;

namespace ChronosPointer.Legacy
{
#if !VALPHA4
    [StaticConstructorOnStartup]
#endif
    public static class ChronosLegacyBootstrap
    {
        static ChronosLegacyBootstrap()
        {
            Log.Message("[ChronosPointer] Loaded legacy compatibility assembly.");
        }
    }
}
