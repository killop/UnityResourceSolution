using System;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public class AudioPreviewGenerator : PreviewGeneratorBase<AudioClip>
    {
        private static readonly Color _audioColor = new Color(255 / 255f, 140 / 255f, 0);
        
        private Type _audioUtilType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AudioUtil");
        private Func<AudioImporter, float[]> _getAudioMinMaxData;
        
        public AudioPreviewGenerator(PreviewRenderer renderer) : base(renderer)
        {
            _getAudioMinMaxData = TypeAccessor.GetMethod(_audioUtilType, "GetMinMaxData")
                .CreateDelegate<Func<AudioImporter, float[]>>();
        }

        protected override bool BeforeRender(AudioClip target)
        {
            // We get the width and height separately to improve the code readability. 
            // I don't know why we need to divide by 2. But it works...
            int width = (int)LibraryPreferences.PreviewResolution / 2;
            int height = (int)LibraryPreferences.PreviewResolution / 2;
            Rect previewRect = new Rect(0, 0, width, height);
            
             // Audio preview
            previewRect = new Rect(
                0.05f * width * EditorGUIUtility.pixelsPerPoint,
                0.05f * width * EditorGUIUtility.pixelsPerPoint, 
                1.9f * width * EditorGUIUtility.pixelsPerPoint, 
                1.9f * height * EditorGUIUtility.pixelsPerPoint);

            var import = (AudioImporter) AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(target));
            
            float scaleFactor = 1.0f * 0.95f; // Reduce amplitude slightly to make highly compressed signals fit.
            float[] minMaxData = import == null ? null : _getAudioMinMaxData(import);
            int channelsCount = target.channels;
            int samplesCount = minMaxData == null ? 0 : minMaxData.Length / (2 * channelsCount);
            float perChannelHeight = previewRect.height / target.channels;

            for (int channel = 0; channel < channelsCount; channel++)
            {
                Rect channelRect = new Rect(previewRect.x, previewRect.y + perChannelHeight * channel, previewRect.width, perChannelHeight);

                AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator eval =
                    (float x, out Color col, out float minValue, out float maxValue) =>
                    {
                        col = _audioColor;
                        if (samplesCount <= 0)
                        {
                            minValue = 0;
                            maxValue = 0;
                        }
                        else
                        {
                            float p = Mathf.Clamp(x * (samplesCount - 2), 0.0f, samplesCount - 2);
                            int i = Mathf.FloorToInt(p);
                            int offset1 = (i * channelsCount + channel) * 2;
                            int offset2 = offset1 + channelsCount * 2;

                            minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]) * scaleFactor;
                            maxValue = Mathf.Min(minMaxData[offset1], minMaxData[offset2]) * scaleFactor;
                        
                            if (minValue > maxValue)
                                (minValue, maxValue) = (maxValue, minValue);   
                        }
                    };
                AudioCurveRendering.DrawMinMaxFilledCurve(channelRect, eval);
            }

            return true;
        }
    }
}
