// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using UnityEngine;
using UnityEditor;

namespace Hextant.Editor
{
    public static class SettingsExtensions
    {
        // The SettingsProvider instance used to display settings in Edit/Preferences
        // and Edit/Project Settings.
        public static SettingsProvider GetSettingsProvider<T>(
            this Settings<T> settings ) where T : Settings<T>
        {
            Debug.Assert( Settings<T>.attribute.displayPath != null );
            return new ScriptableObjectSettingsProvider( settings,
                Settings<T>.attribute.usage == SettingsUsage.EditorUser ?
                SettingsScope.User : SettingsScope.Project,
                Settings<T>.attribute.displayPath );
        }
    }
}
