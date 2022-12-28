// Copyright 2021 by Hextant Studios. https://HextantStudios.com
// This work is licensed under CC BY 4.0. http://creativecommons.org/licenses/by/4.0/
using System;

namespace Hextant
{
    // Specifies the settings type, path in the settings UI, and optionally its
    // filename. If the filename is not set, the type's name is used.
    public sealed class SettingsAttribute : Attribute
    {
        public SettingsAttribute( SettingsUsage usage, string displayPath = null,
            string filename = null )
        {
            this.usage = usage;
            this.filename = filename;
            this.displayPath = displayPath != null ?
                ( ( usage == SettingsUsage.EditorUser ?
                "Preferences/" : "Project/" ) + displayPath ) : null;
        }

        // The type of settings (how and when they are used).
        public readonly SettingsUsage usage;

        // The display name and optional path in the settings dialog.
        public readonly string displayPath;

        // The filename used to store the settings. If null, the type's name is used.
        public readonly string filename;
    }
}
