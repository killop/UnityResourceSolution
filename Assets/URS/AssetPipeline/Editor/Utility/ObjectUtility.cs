using UnityEngine;

namespace Daihenka.AssetPipeline
{
    internal static class ObjectUtility
    {
        public static void SafeDestroy(this Object obj)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        public static void SetLayer(this GameObject parent, int layer, bool recursive = true)
        {
            parent.layer = layer;
            if (!recursive)
            {
                return;
            }

            foreach (var transform in parent.transform.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
            }
        }
    }
}