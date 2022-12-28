using System;
using System.IO;
using Daihenka.AssetPipeline.Import;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Daihenka.AssetPipeline
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetProcessorDescriptionAttribute : Attribute
    {
        readonly string m_Name;
        readonly string m_Icon;
        readonly Type m_IconType;
        Texture m_CachedIcon;
        ImportAssetTypeFlag m_ValidAssetTypes;

        public AssetProcessorDescriptionAttribute(ImportAssetTypeFlag validAssetTypes = ImportAssetTypeFlag.All) : this("", iconType: null, validAssetTypes)
        {
        }

        public AssetProcessorDescriptionAttribute(Type iconType, ImportAssetTypeFlag validAssetTypes = ImportAssetTypeFlag.All) : this("", iconType, validAssetTypes)
        {
        }

        public AssetProcessorDescriptionAttribute(string icon, ImportAssetTypeFlag validAssetTypes = ImportAssetTypeFlag.All) : this("", icon, validAssetTypes)
        {
        }

        public AssetProcessorDescriptionAttribute(string name, Type iconType, ImportAssetTypeFlag validAssetTypes = ImportAssetTypeFlag.All)
        {
            m_Name = name;
            m_Icon = null;
            m_IconType = iconType;
            m_ValidAssetTypes = validAssetTypes;
        }

        public AssetProcessorDescriptionAttribute(string name, string icon, ImportAssetTypeFlag validAssetTypes = ImportAssetTypeFlag.All)
        {
            m_Name = name;
            m_Icon = icon;
            m_IconType = null;
            m_ValidAssetTypes = validAssetTypes;
        }

        public string Name => m_Name;
        public ImportAssetTypeFlag ValidAssetTypes => m_ValidAssetTypes;

        public Texture Icon
        {
            get
            {
                if (m_CachedIcon == null)
                {
                    if (m_IconType != null)
                    {
                        m_CachedIcon = (Texture2D) UnityEditorDynamic.EditorGUIUtility.FindTextureByType(m_IconType);
                    }
                    else if (!string.IsNullOrEmpty(m_Icon))
                    {
                        m_CachedIcon = EditorGUIUtility.FindTexture(m_Icon);
                        if (!m_CachedIcon && File.Exists(m_Icon))
                        {
                            m_CachedIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(m_Icon);
                        }

                        if (!m_CachedIcon)
                        {
                            m_CachedIcon = InternalEditorUtility.FindIconForFile(m_Icon);
                        }

                        if (!m_CachedIcon)
                        {
                            m_CachedIcon = EditorGUIUtility.IconContent(m_Icon).image;
                        }
                    }

                    if (!m_CachedIcon)
                    {
                        m_CachedIcon = EditorGUIUtility.FindTexture("_Popup@2x");
                    }
                }

                return m_CachedIcon;
            }
        }
    }
}