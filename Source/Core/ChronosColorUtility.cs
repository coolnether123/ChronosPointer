using UnityEngine;

namespace ChronosPointer.Core
{
    internal static class ChronosColorUtility
    {
        public static Color Mix(Color first, Color second, float amount, bool includeAlpha = false)
        {
            Vector4 firstVector = new Vector4(first.r, first.g, first.b, first.a);
            Vector4 secondVector = new Vector4(second.r, second.g, second.b, second.a);
            Vector4 mixed = firstVector * (1f - amount) + secondVector * amount;
            return new Color(mixed.x, mixed.y, mixed.z, includeAlpha ? mixed.w : second.a);
        }
    }
}
