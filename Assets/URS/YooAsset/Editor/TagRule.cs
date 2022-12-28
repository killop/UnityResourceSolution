using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Compares an object's name against a string using a matching option.
    /// </summary>
    [Serializable]
    public class TagRule : LibraryRuleBase
    {
        public enum TagRuleType
        {
            ResourceGroup, 
            BundleName,
            BundleCompression,
            CustomTag,
        }

        [SerializeField] private TagRuleType _tagType;
        [SerializeField] private string _text;

        /// <summary>
        /// The text string to match with an object's name.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// The type of matching on an object's name to perform.
        /// </summary>
        public TagRuleType TagType
        {
            get { return _tagType; }
            set { _tagType = value; }
        }

        /// <inheritdoc/>
        public override string SearchQuery
        {
            get
            {
                return string.Empty;
            }
        }

        /// <inheritdoc/>
        public override bool Matches(LibraryItem item)
        {
            return true;
        }

        private bool IsFilterValid()
        {
            return true;
        }
    }
}
