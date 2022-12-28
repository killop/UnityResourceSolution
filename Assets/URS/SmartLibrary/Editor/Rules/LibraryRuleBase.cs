using UnityEngine;

namespace Bewildered.SmartLibrary
{
    /// <summary>
    /// Derive from this class to create a rule that can be used in <see cref="LibraryCollection"/>s.
    /// </summary>
    [System.Serializable]
    public abstract class LibraryRuleBase
    {
        [SerializeField] private OperandType _operand = OperandType.And;
        [SerializeField] private ConditionScopeState _scopeState = ConditionScopeState.None;

        /// <summary>
        /// How the <see cref="LibraryRuleBase"/> is compared within a <see cref="RuleSet"/>.
        /// </summary>
        public OperandType Operand
        {
            get { return _operand; }
            set { _operand = value; }
        }

        /// <summary>
        /// Whether the <see cref="LibraryRuleBase"/> is starts or ends a scope used for comparision between <see cref="LibraryRuleBase"/>s within a <see cref="RuleSet"/>.
        /// </summary>
        public ConditionScopeState ScopeState
        {
            get { return _scopeState; }
            internal set { _scopeState = value; }
        }
        
        /// <summary>
        /// The QuickSearch search query used by <see cref="SmartCollection"/>s to find items. Should return empty if any rule properties are not properly set.
        /// </summary>
        public abstract string SearchQuery { get; }

        /// <summary>
        /// Determines whether the specified <see cref="LibraryItem"/> matches the <see cref="LibraryRuleBase"/>.
        /// </summary>
        /// <remarks>
        /// Default return value should be <c>false</c>, 
        /// only returning <c>true</c> if the <see cref="LibraryItem"/> actually matches the <see cref="LibraryRuleBase"/>.
        /// </remarks>
        /// <param name="item">The <see cref="LibraryItem"/> to check.</param>
        /// <returns><c>true</c> if <paramref name="item"/> matches the <see cref="LibraryRuleBase"/>; otherwise, <c>false</c>.</returns>
        public abstract bool Matches(LibraryItem item);
    }
}