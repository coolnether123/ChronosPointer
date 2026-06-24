namespace ChronosPointer.ModSupport
{
    internal interface IChronosTimeScaleSupport
    {
        bool TryGetTicksPerDay(out long ticksPerDay);
        bool TryGetTicksPerHour(out long ticksPerHour);
    }
}
