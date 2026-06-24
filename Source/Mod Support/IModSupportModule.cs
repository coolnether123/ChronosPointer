namespace ChronosPointer.ModSupport
{
    internal interface IModSupportModule
    {
        string PackageId { get; }
        string DisplayName { get; }
        void OnModsDetected();
    }
}
