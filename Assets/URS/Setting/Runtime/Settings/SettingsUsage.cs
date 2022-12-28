// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
namespace Hextant
{
    // Specifies how the settings are used and when they are available.
    public enum SettingsUsage
    {
        // Project-wide settings available at runtime.
        // Ex: Project Settings/Time
        RuntimeProject,

        // Project-wide settings available only in the editor.
        // Ex: Project Settings/Version Control
        EditorProject,

        // User-specific settings available only in the editor.
        // Ex: Preferences/Scene View
        EditorUser
    }
}
