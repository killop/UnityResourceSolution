// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Hextant
{
    // Base class for project/users settings. Use the [Settings] attribute to
    // specify its usage, display path, and filename.
    // * Settings are stored in Assets/Settings/ folder.
    // * Important! The user settings folder Assets/Settings/Editor/User/ must be
    //   excluded from source control.
    // * User settings will be placed in a subdirectory named the same as
    //   the current project folder so that shallow cloning (symbolic links to
    //   the Assets/ folder) can be used when testing multiplayer games.
    // See: https://HextantStudios.com/unity-custom-settings/
    public abstract class Settings<T> : ScriptableObject where T : Settings<T>
    {
        // The singleton instance. (Not thread safe but fine for ScriptableObjects.)
        public static T instance => _instance != null ? _instance : Initialize();
        static T _instance;

        // Loads or creates the settings instance and stores it in _instance.
        protected static T Initialize()
        {
            // If the instance is already valid, return it. Needed if called from a 
            // derived class that wishes to ensure the settings are initialized.
            if( _instance != null ) return _instance;

            // Verify there was a [Settings] attribute.
            if( attribute == null )
            {
                Debug.LogError( "[Settings] attribute missing for type: " +
                    typeof( T ).Name );
                return null;
            }

            // Attempt to load the settings asset.
            var filename = attribute.filename ?? typeof( T ).Name;
            var path = GetSettingsPath() + filename + ".asset";

            if( attribute.usage == SettingsUsage.RuntimeProject )
                _instance = Resources.Load<T>( filename );
#if UNITY_EDITOR
            else
                _instance = AssetDatabase.LoadAssetAtPath<T>( path );

            // Return the instance if it was the load was successful.
            if( _instance != null ) return _instance;

            // Move settings if its path changed (type renamed or attribute changed)
            // while the editor was running. This must be done manually if the
            // change was made outside the editor.
            var instances = Resources.FindObjectsOfTypeAll<T>();
            if( instances.Length > 0 )
            {
                var oldPath = AssetDatabase.GetAssetPath( instances[ 0 ] );
                var result = AssetDatabase.MoveAsset( oldPath, path );
                if( string.IsNullOrEmpty( result ) )
                    return _instance = instances[ 0 ];
                else
                    Debug.LogWarning( $"Failed to move previous settings asset " +
                        $"'{oldPath}' to '{path}'. " +
                        $"A new settings asset will be created.", _instance );
            }
#endif
            // Create the settings instance if it was not loaded or found.
            if( _instance != null ) return _instance;
            _instance = CreateInstance<T>();

#if UNITY_EDITOR
            // Create a new settings instance if it was not found.
            // Create the directory as Unity does not do this itself.
            Directory.CreateDirectory( Path.Combine(
                Directory.GetCurrentDirectory(),
                Path.GetDirectoryName( path ) ) );

            // Create the asset only in the editor.
            AssetDatabase.CreateAsset( _instance, path );
#endif
            return _instance;
        }

        // Returns the full asset path to the settings file.
        static string GetSettingsPath()
        {
            var path = "Assets/Settings/";

            switch( attribute.usage )
            {
                case SettingsUsage.RuntimeProject:
                    path += "Resources/"; break;
#if UNITY_EDITOR
                case SettingsUsage.EditorProject:
                    path += "Editor/"; break;
                case SettingsUsage.EditorUser:
                    path += "Editor/User/" + GetProjectFolderName() + '/'; break;
#endif
                default: throw new System.InvalidOperationException();
            }
            return path;
        }

        // The derived type's [Settings] attribute.
        public static SettingsAttribute attribute =>
            _attribute != null ? _attribute : _attribute =
                typeof( T ).GetCustomAttribute<SettingsAttribute>( true );
        static SettingsAttribute _attribute;

        // Called to validate settings changes.
        protected virtual void OnValidate() { }

#if UNITY_EDITOR
        // Sets the specified setting to the desired value and marks the settings
        // so that it will be saved.
        protected void Set<S>( ref S setting, S value )
        {
            if( EqualityComparer<S>.Default.Equals( setting, value ) ) return;
            setting = value;
            OnValidate();
            SetDirty();
        }

        // Marks the settings dirty so that it will be saved.
        protected new void SetDirty() => EditorUtility.SetDirty( this );

        // The directory name of the current project folder.
        static string GetProjectFolderName()
        {
            var path = Application.dataPath.Split( '/' );
            return path[ path.Length - 2 ];
        }
#endif

        // Base class for settings contained by a Settings<T> instance.
        [Serializable]
        public abstract class SubSettings
        {
            // Called when a setting is modified.
            protected virtual void OnValidate() { }

#if UNITY_EDITOR
            // Sets the specified setting to the desired value and marks the settings
            // instance so that it will be saved.
            protected void Set<S>( ref S setting, S value )
            {
                if( EqualityComparer<S>.Default.Equals( setting, value ) ) return;
                setting = value;
                OnValidate();
                instance.SetDirty();
            }
#endif
        }
    }
}