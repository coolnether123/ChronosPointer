using UnityEngine;
using Verse;

namespace ChronosPointer
{
    [StaticConstructorOnStartup]
    public static class ChronosPointerTextures
    {
        public static readonly Texture2D ArrowTexture;

        static ChronosPointerTextures()
        {
#if RIMWORLD_1_5
            ArrowTexture = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowRight_1_5", false);
#else
            ArrowTexture = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowRight", false);
#endif

            if (ArrowTexture == null)
            {
                Log.Warning("ChronosPointer: Could not load arrow texture from UI/Widgets/ArrowRight.");
            }
            else
            {
                //Log.Message($"ChronosPointer: Loaded arrow texture ({ArrowTexture.width}x{ArrowTexture.height}).");
            }
        }
    }
}