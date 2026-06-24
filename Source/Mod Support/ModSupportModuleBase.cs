using Verse;

namespace ChronosPointer.ModSupport
{
    internal abstract class ModSupportModuleBase : IModSupportModule
    {
        public abstract string PackageId { get; }
        public virtual string DisplayName => PackageId;

        public virtual void OnModsDetected()
        {
        }

        protected void Warn(string message)
        {
            Log.Warning($"[ChronosPointer][ModSupport][{DisplayName}] {message}");
        }
    }
}
