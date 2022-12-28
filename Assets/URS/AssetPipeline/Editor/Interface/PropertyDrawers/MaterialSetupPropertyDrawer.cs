using System;
using System.Collections.Generic;
using System.Linq;
using Daihenka.AssetPipeline.Filters;
using Daihenka.AssetPipeline.Import;
using Daihenka.AssetPipeline.Processors;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Daihenka.AssetPipeline.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(SetupMaterials.MaterialSetup))]
    internal class MaterialSetupPropertyDrawer : PropertyDrawer
    {
        static readonly Texture s_HiddenIcon = EditorGUIUtility.FindTexture("scenevis_hidden_hover@2x");
        static float singleLineHeight => EditorGUIUtility.singleLineHeight;
        static string[] shaderNames => SetupMaterialsInspector.m_ShaderNames;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propHeights = 0f;
            var propMappings = property.FindPropertyRelative("propertyMappings");
            for (var i = 0; i < propMappings.arraySize; i++) {
                var shaderProperty = propMappings.GetArrayElementAtIndex(i);
                if (IsShaderPropertyType(shaderProperty, ShaderUtil.ShaderPropertyType.TexEnv)) {
                    propHeights += EditorGUI.GetPropertyHeight(shaderProperty.FindPropertyRelative("textureNameFilter"), GetShaderPropertyName(shaderProperty));
                }
                else if (IsShaderPropertyType(shaderProperty, ShaderUtil.ShaderPropertyType.Color)) {
                    propHeights += EditorGUI.GetPropertyHeight(shaderProperty.FindPropertyRelative("colorValue"), GetShaderPropertyName(shaderProperty));
                }
                else if (IsShaderPropertyType(shaderProperty, ShaderUtil.ShaderPropertyType.Vector)) {
                    propHeights += EditorGUI.GetPropertyHeight(shaderProperty.FindPropertyRelative("vectorValue"), GetShaderPropertyName(shaderProperty));
                }
                else if (IsShaderPropertyType(shaderProperty, ShaderUtil.ShaderPropertyType.Float)) {
                    propHeights += EditorGUI.GetPropertyHeight(shaderProperty.FindPropertyRelative("floatValue"), GetShaderPropertyName(shaderProperty));
                }
                else if (IsShaderPropertyType(shaderProperty, ShaderUtil.ShaderPropertyType.Range)) {
                    propHeights += EditorGUI.GetPropertyHeight(shaderProperty.FindPropertyRelative("floatValue"), GetShaderPropertyName(shaderProperty));
                }
            }

            return singleLineHeight * 4 + propHeights + 36 + propMappings.arraySize * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position.x, position.y + 12, position.width, singleLineHeight);
            var propMappings = property.FindPropertyRelative("propertyMappings");

            EditorGUI.PropertyField(rect, property.FindPropertyRelative("materialFilter"), DaiGUIContent.materialNameFilter);
            rect.y += singleLineHeight + 2;

            DrawShaderDropdown(rect, property, propMappings);
            rect.y += singleLineHeight + 2;

            EditorGUI.PropertyField(rect, property.FindPropertyRelative("textureSearch"));
            rect.y += singleLineHeight + 2;

            rect.y += 6;
            if (propMappings.arraySize == 0)
            {
                EditorGUI.LabelField(rect, "No Material Properties", DaiGUIStyles.boldLabelCentered);
                rect.y += singleLineHeight + 1;
            }
            else
            {
                EditorGUI.LabelField(rect, "Material Properties", DaiGUIStyles.boldLabel);
                rect.y += singleLineHeight + 1;
                for (var i = 0; i < propMappings.arraySize; i++)
                {
                    var propMap = propMappings.GetArrayElementAtIndex(i);
                    rect.y += DrawShaderProperty(rect, propMap) + 2;
                }
            }
        }

        static float DrawShaderProperty(Rect rect, SerializedProperty property)
        {
            var height = 0f;
            
            EditorGUI.PropertyField(rect, property.FindPropertyRelative("overridden"), GUIContent.none);
            GUI.enabled = property.FindPropertyRelative("overridden").boolValue;
            var rectPropertyField = new Rect(rect.x + 18, rect.y, rect.width - 18, rect.height);

            if (IsShaderPropertyType(property, ShaderUtil.ShaderPropertyType.TexEnv))
            {
                var isSet = !string.IsNullOrEmpty(property.FindPropertyRelative("textureNameFilter").FindPropertyRelative("pattern").stringValue);
                height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("textureNameFilter"), GetShaderPropertyName(property));
                EditorGUI.PropertyField(rectPropertyField, property.FindPropertyRelative("textureNameFilter"), GetShaderPropertyName(property));
                EditorGUI.DrawRect(new Rect(rect.x - 14, rect.y, 4, rect.height), AssetPipelineSettings.GetStatusColor(isSet));
                DrawTexture(rect, AssetImportPipeline.AssetTypeIcons[ImportAssetType.Textures]);
                DrawHiddenTexture(rect, property, 22);
            }
            else if (IsShaderPropertyType(property, ShaderUtil.ShaderPropertyType.Color))
            {
                height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("colorValue"), GetShaderPropertyName(property));
                EditorGUI.PropertyField(rectPropertyField, property.FindPropertyRelative("colorValue"), GetShaderPropertyName(property));
                DrawHiddenTexture(rect, property);
            }
            else if (IsShaderPropertyType(property, ShaderUtil.ShaderPropertyType.Vector))
            {
                height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("vectorValue"), GetShaderPropertyName(property));
                var vectorProp = property.FindPropertyRelative("vectorValue");
                vectorProp.vector4Value = EditorGUI.Vector4Field(rectPropertyField, GetShaderPropertyName(property), vectorProp.vector4Value);
                DrawHiddenTexture(rect, property);

            }
            else if (IsShaderPropertyType(property, ShaderUtil.ShaderPropertyType.Float))
            {
                var propName = property.FindPropertyRelative("materialPropertyName").stringValue;
                height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("floatValue"), GetShaderPropertyName(property));
                var floatProp = property.FindPropertyRelative("floatValue");
                var propLabel = GetShaderPropertyName(property);
                switch (propName)
                {
                    case "_SrcBlend":
                    case "_DstBlend":
                        floatProp.floatValue = EditorGUI.IntPopup(rectPropertyField, propLabel, (int) floatProp.floatValue, Enum.GetNames(typeof(BlendMode)).Select(x => new GUIContent(x)).ToArray(), (int[]) Enum.GetValues(typeof(BlendMode)));
                        break;
                    case "_AlphaClip":
                    case "_ZWrite":
                        floatProp.floatValue = EditorGUI.Toggle(rectPropertyField, propLabel, (int) floatProp.floatValue == 1) ? 1f : 0f;
                        break;
                    case "_Surface":
                        floatProp.floatValue = EditorGUI.IntPopup(rectPropertyField, propLabel, (int) floatProp.floatValue, Enum.GetNames(typeof(SurfaceType)).Select(x => new GUIContent(x)).ToArray(), (int[]) Enum.GetValues(typeof(SurfaceType)));
                        break;
                    case "_Cull":
                        floatProp.floatValue = EditorGUI.IntPopup(rectPropertyField, propLabel, (int) floatProp.floatValue, Enum.GetNames(typeof(CullMode)).Select(x => new GUIContent(x)).ToArray(), (int[]) Enum.GetValues(typeof(CullMode)));
                        break;
                    default:
                        EditorGUI.PropertyField(rectPropertyField, floatProp, propLabel);
                        break;
                }

                DrawHiddenTexture(rect, property);
            }
            else if (IsShaderPropertyType(property, ShaderUtil.ShaderPropertyType.Range))
            {
                height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("floatValue"), GetShaderPropertyName(property));
                EditorGUI.Slider(rectPropertyField, property.FindPropertyRelative("floatValue"), property.FindPropertyRelative("minRange").floatValue, property.FindPropertyRelative("maxRange").floatValue, GetShaderPropertyName(property));
                DrawHiddenTexture(rect, property);
            }
            
            GUI.enabled = true;
            return height;
        }

        static void DrawHiddenTexture(Rect rect, SerializedProperty prop, int offset = 2)
        {
            if (prop.FindPropertyRelative("isHidden").boolValue)
                DrawTexture(rect, s_HiddenIcon, offset);
        }

        static void DrawTexture(Rect rect, Texture assetTypeIcon, float offset = 2)
        {
            GUI.DrawTexture(new Rect(rect.x + EditorGUIUtility.labelWidth + 2f - offset, rect.y + (rect.height - 16) * 0.5f, 16, 16), assetTypeIcon);
        }

        static GUIContent GetShaderPropertyName(SerializedProperty prop)
        {
            var propLabel = prop.FindPropertyRelative("materialPropertyDescription").stringValue;
            return new GUIContent(string.IsNullOrEmpty(propLabel) ? prop.FindPropertyRelative("materialPropertyName").stringValue : propLabel);
        }

        static bool IsShaderPropertyType(SerializedProperty prop, ShaderUtil.ShaderPropertyType type)
        {
            return prop.FindPropertyRelative("materialPropertyType").enumValueIndex == (int) type;
        }

        static void DrawShaderDropdown(Rect rect, SerializedProperty property, SerializedProperty propMappings)
        {
            var shaderProp = property.FindPropertyRelative("shader");
            var shader = (Shader) shaderProp.objectReferenceValue;
            var shaderIndex = shader ? ArrayUtility.IndexOf(shaderNames, shader.name) : -1;
            var newShaderIndex = EditorGUI.Popup(rect, "Shader", shaderIndex, shaderNames);
            if (newShaderIndex != shaderIndex)
            {
                shader = Shader.Find(shaderNames[newShaderIndex]);
                shaderProp.objectReferenceValue = shader;
                UpdateMaterialPropertyMappings(property, propMappings, shader);
            }
        }

        static void UpdateMaterialPropertyMappings(SerializedProperty property, SerializedProperty propMappings, Shader shader)
        {
            property.serializedObject.ApplyModifiedProperties();
            var propMap = propMappings.GetSerializedValue<List<SetupMaterials.MaterialTextureMap>>();
            propMap.Clear();
            for (var i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propName = ShaderUtil.GetPropertyName(shader, i);
                var propDesc = ShaderUtil.GetPropertyDescription(shader, i);
                var propType = ShaderUtil.GetPropertyType(shader, i);
                var isHidden = ShaderUtil.IsShaderPropertyHidden(shader, i);
                if (propType == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    propMap.Add(new SetupMaterials.MaterialTextureMap(propName, propDesc, new PathFilter {pattern = ""}, isHidden));
                }
                else if (propType == ShaderUtil.ShaderPropertyType.Color)
                {
                    propMap.Add(new SetupMaterials.MaterialTextureMap(propName, propDesc, ShaderUtil.ShaderPropertyType.Color, shader.GetPropertyDefaultVectorValue(i), isHidden));
                }
                else if (propType == ShaderUtil.ShaderPropertyType.Vector)
                {
                    propMap.Add(new SetupMaterials.MaterialTextureMap(propName, propDesc, ShaderUtil.ShaderPropertyType.Vector, shader.GetPropertyDefaultVectorValue(i), isHidden));
                }
                else if (propType == ShaderUtil.ShaderPropertyType.Float)
                {
                    propMap.Add(new SetupMaterials.MaterialTextureMap(propName, propDesc, shader.GetPropertyDefaultFloatValue(i), isHidden));
                }
                else if (propType == ShaderUtil.ShaderPropertyType.Range)
                {
                    var value = ShaderUtil.GetRangeLimits(shader, i, 0);
                    var min = ShaderUtil.GetRangeLimits(shader, i, 1);
                    var max = ShaderUtil.GetRangeLimits(shader, i, 2);
                    propMap.Add(new SetupMaterials.MaterialTextureMap(propName, propDesc, value, min, max, isHidden));
                }
            }

            property.serializedObject.Update();
        }

        enum SurfaceType
        {
            Opaque,
            Transparent
        }
    }
}
