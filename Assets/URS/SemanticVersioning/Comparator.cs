using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SemanticVersioning
{
    internal class Comparator : IEquatable<Comparator>
    {
        public readonly Operator ComparatorType;

        public readonly SemanticVersion Version;

        private const string pattern = @"
            \s*
            ([=<>]*)                # Comparator type (can be empty)
            \s*
            ([0-9a-zA-Z\-\+\.\*]+)  # Version (potentially partial version)
            \s*
            ";

        public Comparator(string input)
        {
            var regex = new Regex(String.Format("^{0}$", pattern),
                    RegexOptions.IgnorePatternWhitespace);
            var match = regex.Match(input);
            if (!match.Success)
            {
                throw new ArgumentException(String.Format("Invalid comparator string: {0}", input));
            }

            ComparatorType = ParseComparatorType(match.Groups[1].Value);
            var partialVersion = new PartialVersion(match.Groups[2].Value);

            if (!partialVersion.IsFull())
            {
                // For Operator.Equal, partial versions are handled by the StarRange
                // desugarer, and desugar to multiple comparators.

                switch (ComparatorType)
                {
                    // For <= with a partial version, eg. <=1.2.x, this
                    // means the same as < 1.3.0, and <=1.x means <2.0
                    case Operator.LessThanOrEqual:
                        ComparatorType = Operator.LessThan;
                        if (!partialVersion.Major.HasValue)
                        {
                            // <=* means >=0.0.0
                            ComparatorType = Operator.GreaterThanOrEqual;
                            Version = new SemanticVersion(0, 0, 0);
                        }
                        else if (!partialVersion.Minor.HasValue)
                        {
                            Version = new SemanticVersion(partialVersion.Major.Value + 1, 0, 0);
                        }
                        else
                        {
                            Version = new SemanticVersion(partialVersion.Major.Value, partialVersion.Minor.Value + 1, 0);
                        }
                        break;
                    case Operator.GreaterThan:
                        ComparatorType = Operator.GreaterThanOrEqualIncludingPrereleases;
                        if (!partialVersion.Major.HasValue)
                        {
                            // >* is unsatisfiable, so use <0.0.0
                            ComparatorType = Operator.LessThan;
                            Version = new SemanticVersion(0, 0, 0);
                        }
                        else if (!partialVersion.Minor.HasValue)
                        {
                            // eg. >1.x -> >=2.0
                            Version = new SemanticVersion(partialVersion.Major.Value + 1, 0, 0);
                        }
                        else
                        {
                            // eg. >1.2.x -> >=1.3
                            Version = new SemanticVersion(partialVersion.Major.Value, partialVersion.Minor.Value + 1, 0);
                        }
                        break;
                    case Operator.LessThan:
                        // <1.2.x means <1.2.0 but not allowing 1.2.0 prereleases if includePrereleases is used
                        ComparatorType = Operator.LessThanExcludingPrereleases;
                        Version = partialVersion.ToZeroVersion();
                        break;
                    case Operator.GreaterThanOrEqual:
                        // >=1.2.x means >=1.2.0 and includes 1.2.0 prereleases if includePrereleases is used
                        ComparatorType = Operator.GreaterThanOrEqualIncludingPrereleases;
                        Version = partialVersion.ToZeroVersion();
                        break;
                    default:
                        Version = partialVersion.ToZeroVersion();
                        break;
                }
            }
            else
            {
                Version = partialVersion.ToZeroVersion();
            }
        }

        public Comparator(Operator comparatorType, SemanticVersion comparatorVersion)
        {
            if (comparatorVersion == null)
            {
                throw new NullReferenceException("Null comparator version");
            }
            ComparatorType = comparatorType;
            Version = comparatorVersion;
        }

        public static Tuple<int, Comparator> TryParse(string input)
        {
            var regex = new Regex(String.Format("^{0}", pattern),
                    RegexOptions.IgnorePatternWhitespace);

            var match = regex.Match(input);

            return match.Success ?
                Tuple.Create(
                    match.Length,
                    new Comparator(match.Value))
                : null;
        }

        private static Operator ParseComparatorType(string input)
        {
            switch (input)
            {
                case (""):
                case ("="):
                    return Operator.Equal;
                case ("<"):
                    return Operator.LessThan;
                case ("<="):
                    return Operator.LessThanOrEqual;
                case (">"):
                    return Operator.GreaterThan;
                case (">="):
                    return Operator.GreaterThanOrEqual;
                default:
                    throw new ArgumentException(String.Format("Invalid comparator type: {0}", input));
            }
        }

        public bool IsSatisfied(SemanticVersion version)
        {
            switch(ComparatorType)
            {
                case Operator.Equal:
                    return version == Version;
                case Operator.LessThan:
                    return version < Version;
                case Operator.LessThanOrEqual:
                    return version <= Version;
                case Operator.GreaterThan:
                    return version > Version;
                case Operator.GreaterThanOrEqual:
                    return version >= Version;
                case Operator.GreaterThanOrEqualIncludingPrereleases:
                    return version >= Version || (version.IsPreRelease && version.BaseVersion() == Version);
                case Operator.LessThanExcludingPrereleases:
                    return version < Version && !(version.IsPreRelease && version.BaseVersion() == Version);
                default:
                    throw new InvalidOperationException("Comparator type not recognised.");
            }
        }

        public bool Intersects(Comparator other)
        {
            Func<Comparator, bool> operatorIsGreaterThan = c =>
                c.ComparatorType == Operator.GreaterThan ||
                c.ComparatorType == Operator.GreaterThanOrEqual ||
                c.ComparatorType == Operator.GreaterThanOrEqualIncludingPrereleases;
            Func<Comparator, bool> operatorIsLessThan = c =>
                c.ComparatorType == Operator.LessThan ||
                c.ComparatorType == Operator.LessThanOrEqual ||
                c.ComparatorType == Operator.LessThanExcludingPrereleases;
            Func<Comparator, bool> operatorIncludesEqual = c =>
                c.ComparatorType == Operator.GreaterThanOrEqual ||
                c.ComparatorType == Operator.GreaterThanOrEqualIncludingPrereleases ||
                c.ComparatorType == Operator.Equal ||
                c.ComparatorType == Operator.LessThanOrEqual;

            if (this.Version > other.Version && (operatorIsLessThan(this) || operatorIsGreaterThan(other)))
                return true;

            if (this.Version < other.Version && (operatorIsGreaterThan(this) || operatorIsLessThan(other)))
                return true;

            if (this.Version == other.Version && (
                (operatorIncludesEqual(this) && operatorIncludesEqual(other)) ||
                (operatorIsLessThan(this) && operatorIsLessThan(other)) ||
                (operatorIsGreaterThan(this) && operatorIsGreaterThan(other))
            ))
                return true;

            return false;
        }

        public enum Operator
        {
            Equal = 0,
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual,
            GreaterThanOrEqualIncludingPrereleases,
            LessThanExcludingPrereleases,
        }

        public override string ToString()
        {
            string operatorString = null;
            switch(ComparatorType)
            {
                case Operator.Equal:
                    operatorString = "=";
                    break;
                case Operator.LessThan:
                case Operator.LessThanExcludingPrereleases:
                    operatorString = "<";
                    break;
                case Operator.LessThanOrEqual:
                    operatorString = "<=";
                    break;
                case Operator.GreaterThan:
                    operatorString = ">";
                    break;
                case Operator.GreaterThanOrEqual:
                case Operator.GreaterThanOrEqualIncludingPrereleases:
                    operatorString = ">=";
                    break;
                default:
                    throw new InvalidOperationException("Comparator type not recognised.");
            }
            return String.Format("{0}{1}", operatorString, Version);
        }

        public bool Equals(Comparator other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            return ComparatorType == other.ComparatorType && Version == other.Version;
        }

        public override bool Equals(object other)
        {
            return Equals(other as Comparator);
        }

        public override int GetHashCode()
        {
            return new { ComparatorType, Version }.GetHashCode();
        }
    }
}
