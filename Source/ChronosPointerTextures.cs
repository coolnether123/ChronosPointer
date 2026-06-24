using UnityEngine;
using Verse;

namespace ChronosPointer
{
    [StaticConstructorOnStartup]
    public static class ChronosPointerTextures
    {
        private const int DownArrowWidth = 8;
        private const int DownArrowHeight = 8;
        private const int DownArrowTipWidth = 2;

        public static readonly Texture2D ArrowTexture;
        public static readonly Texture2D DownArrowTexture;

        static ChronosPointerTextures()
        {

            ArrowTexture = ContentFinder<Texture2D>.Get("UI/Widgets/ArrowRight", false);
            DownArrowTexture = CreateDownArrowTexture();

            if (ArrowTexture == null)
            {
                Log.Warning("ChronosPointer: Could not load arrow texture from UI/Widgets/ArrowRight.");
            }
            
        }

        private static Texture2D CreateDownArrowTexture()
        {
            Texture2D texture = new Texture2D(DownArrowWidth, DownArrowHeight, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < DownArrowHeight; y++)
            {
                for (int x = 0; x < DownArrowWidth; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int screenY = 0; screenY < DownArrowHeight; screenY++)
            {
                int rowWidth = Mathf.Max(DownArrowTipWidth, DownArrowWidth - ((screenY / 2) * 2));
                int rowStart = (DownArrowWidth - rowWidth) / 2;
                int textureY = DownArrowHeight - 1 - screenY;

                for (int x = rowStart; x < rowStart + rowWidth; x++)
                {
                    texture.SetPixel(x, textureY, Color.white);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
