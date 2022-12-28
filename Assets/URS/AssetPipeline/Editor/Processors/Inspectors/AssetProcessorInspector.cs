using System.Collections.Generic;
using Daihenka.AssetPipeline.NamingConvention;
using UnityEditor;

namespace Daihenka.AssetPipeline.Import
{
    public class AssetProcessorInspector : Editor
    {
        protected AssetProcessor m_ProcessorTarget;
        protected SerializedProperty m_RunOnImportProperty;

        protected virtual void OnEnable()
        {
            m_ProcessorTarget = (AssetProcessor) target;
            m_RunOnImportProperty = serializedObject.FindProperty("runOnImport");
        }

        protected void DrawBaseProperties()
        {
            EditorGUILayout.PropertyField(m_RunOnImportProperty, DaiGUIContent.runOnEveryImport);
            if (!m_RunOnImportProperty.boolValue)
            {
                EditorGUILayout.HelpBox("This processor will only execute when a new asset is added.\nTo run on every import, tick the Run On Every Import toggle above.", MessageType.Warning);
            }

            EditorGUILayout.Space();
        }

        protected void DrawTemplateVariables()
        {
            List<string> profileVariables;
            List<string> filterVariables;
            try
            {
                profileVariables = m_ProcessorTarget.parent.parent.path.Variables;
            }
            catch (ValueException)
            {
                profileVariables = new List<string>();
            }

            try
            {
                filterVariables = m_ProcessorTarget.parent.file.Variables;
            }
            catch (ValueException)
            {
                filterVariables = new List<string>();
            }

            var message = "The following tokens can be used:\n{assetFilename}\t\t\tAsset Filename\n{assetFileExtension}\t\tAsset File Extension\n{assetFolderName}\t\t\tAsset Folder Name\n{assetParentFolderName}\t\tAsset Parent Folder Name\n{assetParentParentFolderName}\tAsset Parent Parent Folder Name";
            foreach (var variable in profileVariables)
            {
                message += $"\n{{{variable}}}";
            }

            foreach (var variable in filterVariables)
            {
                message += $"\n{{{variable}}}";
            }

            EditorGUILayout.HelpBox(message, MessageType.None);
        }
    }
}