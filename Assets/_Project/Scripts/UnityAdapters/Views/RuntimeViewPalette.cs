using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    internal static class RuntimeViewPalette
    {
        public static Color ColorForId(string id)
        {
            uint hash = 2166136261;
            foreach (char character in id)
            {
                hash = (hash ^ character) * 16777619;
            }

            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.55f, 0.92f);
        }
    }
}
