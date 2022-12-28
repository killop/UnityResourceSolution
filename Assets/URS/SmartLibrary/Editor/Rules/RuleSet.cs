using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Bewildered.SmartLibrary
{
    public enum OperandType 
    {
        And = 2,
        AndNot = 4,
        Or = 1,
        OrNot = 3,
    }

    [Flags]
    public enum ConditionScopeState 
    {
        None = 0,
        Open = 1,
        Close  = 2
    }

    [Serializable]
    public class RuleSet : IEnumerable<LibraryRuleBase>, IReadOnlyList<LibraryRuleBase>
    {
        private enum OperatorType
        {
            Open = 0,
            Or = 1,
            And = 2,
            Not = 3
        }

        [SerializeReference] private List<LibraryRuleBase> _rules = new List<LibraryRuleBase>();

        /// <summary>
        /// The number of <see cref="LibraryRuleBase"/>s in the <see cref="RuleSet"/>.
        /// </summary>
        public int Count
        {
            get { return _rules.Count; }
        }

        public LibraryRuleBase this[int index]
        {
            get { return _rules[index]; }
        }

        public RuleSet() { }

        /// <summary>
        /// Add the specified values evaluated with the <c>&amp;&amp;</c> operand to the <see cref="RuleSet"/>.
        /// <code>...&amp;&amp; values[0] &amp;&amp; values[1] &amp;&amp; values[2]...</code>
        /// </summary>
        /// <remarks>The first operand is omitted if no other values have been added to the <see cref="RuleSet"/>.</remarks>
        /// <param name="rules">The values to add to the <see cref="RuleSet"/>.</param>
        /// <returns>The <see cref="RuleSet"/>.</returns>
        public RuleSet And(params LibraryRuleBase[] rules)
        {
            AddConditional(OperandType.And, rules);

            return this;
        }

        /// <summary>
        /// Add the specified values evaluated with the <c>&amp;&amp; !</c> operand to the <see cref="RuleSet"/>.
        /// <code>...&amp;&amp; !values[0] &amp;&amp; !values[1] &amp;&amp; !values[2]...</code>
        /// </summary>
        /// <remarks>The first operand is omitted if no other values have been added to the <see cref="RuleSet"/>.</remarks>
        /// <param name="rules">The values to add to the <see cref="RuleSet"/>.</param>
        /// <returns>The <see cref="RuleSet"/>.</returns>
        public RuleSet AndNot(params LibraryRuleBase[] rules)
        {
            AddConditional(OperandType.AndNot, rules);

            return this;
        }

        /// <summary>
        /// Add the specified values evaluated with the <c>||</c> operand to the <see cref="RuleSet"/>.
        /// <code>...|| values[0] || values[1] || values[2]...</code>
        /// </summary>
        /// <remarks>The first operand is omitted if no other values have been added to the <see cref="RuleSet"/>.</remarks>
        /// <param name="rules">The values to add to the <see cref="RuleSet"/>.</param>
        /// <returns>The <see cref="RuleSet"/>.</returns>
        public RuleSet Or(params LibraryRuleBase[] rules)
        {
            AddConditional(OperandType.Or, rules);

            return this;
        }

        /// <summary>
        /// Add the specified values evaluated with the <c>|| !</c> operand to the <see cref="RuleSet"/>.
        /// <code>...|| !values[0] || !values[1] || !values[2]...</code>
        /// </summary>
        /// <remarks>The first operand is omitted if no other values have been added to the <see cref="RuleSet"/>.</remarks>
        /// <param name="rules">The values to add to the <see cref="RuleSet"/>.</param>
        /// <returns>The <see cref="RuleSet"/>.</returns>
        public RuleSet OrNot(params LibraryRuleBase[] rules)
        {
            AddConditional(OperandType.OrNot, rules);

            return this;
        }

        /// <summary>
        /// Add the values from the action within a scope <c>&amp;&amp; (...action...)</c>.
        /// </summary>
        /// <param name="scope"></param>
        /// <returns>The <see cref="RuleSet"/>.</returns>
        public RuleSet AndScope(Action<RuleSet> scope)
        {
            AddScope(OperandType.And, scope);

            return this;
        }

        public RuleSet AndNotScope(Action<RuleSet> scope)
        {
            AddScope(OperandType.AndNot, scope);

            return this;
        }

        public RuleSet OrScope(Action<RuleSet> scope)
        {
            AddScope(OperandType.Or, scope);

            return this;
        }

        public RuleSet OrNotScope(Action<RuleSet> scope)
        {
            AddScope(OperandType.OrNot, scope);

            return this;
        }

        private void AddConditional(OperandType operand, params LibraryRuleBase[] rules)
        {
            if (rules.Length == 0)
                return;

            foreach (LibraryRuleBase rule in rules)
            {
                rule.Operand = operand;

                _rules.Add(rule);
            }
        }

        private void AddScope(OperandType operand, Action<RuleSet> scope)
        {
            if (scope == null)
                throw new ArgumentNullException("Canot have a null scope");

            int previousCount = _rules.Count;
            scope(this);
            if (_rules.Count > previousCount)
            {
                _rules[previousCount].Operand = operand;
                _rules[previousCount].ScopeState = ConditionScopeState.Open;
                _rules[_rules.Count - 1].ScopeState |= ConditionScopeState.Close;
            }
        }

        public void Clear()
        {
            _rules.Clear();
        }

        /// <summary>
        /// Returns a QuickSearch search query from the rules in the <see cref="RuleSet"/>.
        /// </summary>
        public string GetSearchQuery()
        {
            string fullSearchQuery = "";

            foreach (var rule in _rules)
            {
                // Add a space between this search and the last if there was one.
                if (!string.IsNullOrEmpty(fullSearchQuery))
                    fullSearchQuery += " ";

                var searchQuery = rule.SearchQuery;
                bool isValidFilterQuery = !string.IsNullOrEmpty(searchQuery) && !searchQuery.EndsWith(":");

                if (!string.IsNullOrEmpty(fullSearchQuery) && isValidFilterQuery)
                {
                    if (rule.Operand == OperandType.And || rule.Operand == OperandType.AndNot)
                        fullSearchQuery += "and ";

                    if (rule.Operand == OperandType.Or || rule.Operand == OperandType.OrNot)
                        fullSearchQuery += "or ";
                }

                if (isValidFilterQuery)
                {
                    if (rule.Operand == OperandType.AndNot || rule.Operand == OperandType.OrNot)
                        fullSearchQuery += "-";
                }

                if (rule.ScopeState == ConditionScopeState.Open)
                    fullSearchQuery += "(";

                fullSearchQuery += searchQuery;

                if (rule.ScopeState == ConditionScopeState.Close)
                {
                    if (fullSearchQuery[fullSearchQuery.Length - 1] == '(')
                        fullSearchQuery.Remove(fullSearchQuery.Length - 1);
                    else
                        fullSearchQuery += ")";
                }
            }

            return fullSearchQuery;
        }

        public bool Evaluate(UnityEngine.Object obj)
        {
            return Evaluate(LibraryItem.GetItemInstance(obj));
        }

        /// <summary>
        /// Determines whether the specified <see cref="LibraryItem"/> matches all the rules in the <see cref="RuleSet"/>.
        /// </summary>
        /// <param name="item">The <see cref="LibraryItem"/> to evaluate.</param>
        /// <returns><c>true</c> if <paramref name="item"/> matches all rules; otherwise <c>false</c>.</returns>
        public bool Evaluate(LibraryItem item)
        {
            var results = new Stack<bool>();
            var operators = new Stack<OperatorType>();
            bool first = true;

            if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(item.GUID)))
                return false;
            // We allow all items by default if there are no rules.
            if (_rules.Count == 0)
                return true;

            if (_rules.Count == 1)
            {
                if (_rules[0].Operand == OperandType.OrNot || _rules[0].Operand == OperandType.AndNot)
                    return !_rules[0].Matches(item);
                else
                    return _rules[0].Matches(item);
            }

            foreach (var rule in _rules)
            {
                while (operators.Count > 0 && operators.Count != 1 && operators.Peek() != OperatorType.Not)
                {
                    if ((OperatorType)rule.Operand <= operators.Peek())
                    {
                        EvaluateTopOperands(operators, results);
                        continue;
                    }
                    break;
                }

                // Convert the rule's operand (And AndNot, Or, OrNot) to an operator (And, Or).
                var ruleOperator = rule.Operand == OperandType.And || rule.Operand == OperandType.AndNot
                    ? OperatorType.And
                    : OperatorType.Or;
                
                // We don't push the operand of the first rule because it has no left side condition. e.g. if (&& firstRule)
                if (first)
                    first = false;
                else
                    operators.Push(ruleOperator);

                if (rule.Operand == OperandType.OrNot || rule.Operand == OperandType.AndNot)
                    operators.Push(OperatorType.Not);

                if (rule.ScopeState.HasFlag(ConditionScopeState.Open))
                    operators.Push(OperatorType.Open);

                // Value.
                results.Push(rule.Matches(item));

                if (rule.ScopeState.HasFlag(ConditionScopeState.Close))
                {
                    // Process all the operators between the last 'Open' operand and this 'Close' operand to result in a single value.
                    while (operators.Count > 0 && operators.Peek() != OperatorType.Open)
                    {
                        EvaluateTopOperands(operators, results);
                    }

                    if (operators.Peek() == OperatorType.Open)
                        operators.Pop();
                }
            }

            // Process the remaining operators and thus values.
            while (operators.Count > 0)
            {
                EvaluateTopOperands(operators, results);
            }

            return results.Peek();
        }

        /// <summary>
        /// Evalues the top two operators and pushes the result to the results stack.
        /// </summary>
        private static void EvaluateTopOperands(Stack<OperatorType> operators, Stack<bool> results)
        {
            // NOTE: The order of these operations is very important and should not be changed.
            // Since the collections are stacks, the top value will be the one furthest right. So we 'read' the values 'backwords'.

            var right = results.Pop();
            var left = results.Pop();

            var operand = operators.Pop();

            if (operand == OperatorType.Not)
            {
                right = !right;
                operand = operators.Pop();
            }

            if (operators.Count > 0 && operators.Peek() == OperatorType.Not)
            {
                left = !left;
                operators.Pop();
            }

            switch (operand)
            {
                case OperatorType.Or:
                    results.Push(left || right);
                    break;
                case OperatorType.And:
                    results.Push(left && right);
                    break;
            }
        }

        public IEnumerator<LibraryRuleBase> GetEnumerator()
        {
            return _rules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_rules).GetEnumerator();
        }
    }
}