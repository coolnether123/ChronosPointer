#if VALPHA4
using System;

namespace Verse
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StaticConstructorOnStartupAttribute : Attribute
    {
    }

    public class Window
    {
    }
}

namespace RimWorld
{
    public enum Season
    {
        Undefined = 0
    }
}

namespace RimWorld.Planet
{
}
#endif
