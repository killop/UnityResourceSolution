using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class TextureUtility
    {
        public static Texture2D LoadImage(int width, int height, byte[] bytes)
        {
            try
            {
                var t = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
                t.LoadImage(bytes);
                return t;
            }
            catch
            {
                var t = new Texture2D(1, 1);
                t.SetPixel(0, 0, Color.magenta);
                t.Apply();
                return t;
            }
        }
    }
}