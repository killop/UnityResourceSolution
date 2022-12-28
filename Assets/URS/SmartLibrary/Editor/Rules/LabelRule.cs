using System;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Checks if an object has the specified asset label.
    /// </summary>
    public class LabelRule : LibraryRuleBase
    {
        [SerializeField] private string _label;

        /// <summary>
        /// The asset label that an asset must have.
        /// </summary>
        public string Label
        {
            get { return _label; }
            set { _label = value; }
        }

        /// <inheritdoc/>
        public override string SearchQuery
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_label))
                    return string.Empty;

                return $"l:{_label}";
            }
        }

        /// <inheritdoc/>
        public override bool Matches(LibraryItem item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item), "Item is null");

            var labels = AssetDatabase.GetLabels(new GUID(item.GUID));
            foreach (var label in labels)
            {
                if (label.Equals(_label, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    } 
}
