using System;
using System.IO;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Check's if an object's file path ends with a specific extention.
    /// </summary>
    public class ExtensionRule : LibraryRuleBase
    {
        [SerializeField] private string _extension;

        /// <summary>
        /// The file extention to check for.
        /// </summary>
        public string Extension
        {
            get { return _extension; }
            set { _extension = value; }
        }

        /// <inheritdoc/>
        public override string SearchQuery
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_extension))
                    return string.Empty;

                return $"ext:{GetFixedExtension()}";
            }
        }

        /// <inheritdoc/>
        public override bool Matches(LibraryItem item)
        {
            var itemExtension = Path.GetExtension(item.AssetPath).TrimStart('.');

            return itemExtension.Equals(GetFixedExtension(), StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetFixedExtension()
        {
            string fixedExtension = _extension;

            // Remove the period if the extension string has it as it is not needed.
            if (fixedExtension.StartsWith("."))
                fixedExtension = fixedExtension.TrimStart('.');

            if (fixedExtension.Contains(" "))
                fixedExtension = fixedExtension.Substring(0, fixedExtension.IndexOf(' '));

            return fixedExtension;
        }
    } 
}
