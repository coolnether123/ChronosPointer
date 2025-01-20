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

            ArrowTexture = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowRight", false);

            if (ArrowTexture == null)
            {
                Log.Warning("ChronosPointer: Could not load arrow texture from UI/Widgets/ArrowRight.");
            }
            
        }
    }
}