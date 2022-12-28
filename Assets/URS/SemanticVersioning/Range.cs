using System;
using System.Collections.Generic;
using System.Linq;

namespace SemanticVersioning
{
    /// <summary>
    /// Specifies valid versions.
    /// </summary>
    public class Range : IEquatable<Range>
    {
        private readonly ComparatorSet[] _comparatorSets;

        private readonly string _rangeSpec;

        /// <summary>
        /// Construct a new range from a range specification.
        /// </summary>
        /// <param name="rangeSpec">The range specification string.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <exception cref="System.ArgumentException">Thrown when the range specification is invalid.</exception>
        public Range(string rangeSpec, bool loose = false)
        {
            _rangeSpec = rangeSpec;
            var comparatorSetSpecs = rangeSpec.Split(new[] { "||" }, StringSplitOptions.None);
            _comparatorSets = comparatorSetSpecs.Select(s => new ComparatorSet(s)).ToArray();
        }

        private Range(IEnumerable<ComparatorSet> comparatorSets)
        {
            _comparatorSets = comparatorSets.ToArray();
            _rangeSpec = string.Join(" || ", _comparatorSets.Select(cs => cs.ToString()).ToArray());
        }

        /// <summary>
        /// Determine whether the given version satisfies this range.
        /// </summary>
        /// <param name="version">The version to check.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>true if the range is satisfied by the version.</returns>
        public bool IsSatisfied(SemanticVersion version, bool includePrerelease = false)
        {
            return _comparatorSets.Any(s => s.IsSatisfied(version, includePrerelease: includePrerelease));
        }

        /// <summary>
        /// Determine whether the given version satisfies this range.
        /// With an invalid version this method returns false.
        /// </summary>
        /// <param name="versionString">The version to check.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>true if the range is satisfied by the version.</returns>
        public bool IsSatisfied(string versionString, bool loose = false, bool includePrerelease = false)
        {
            try
            {
                var version = new SemanticVersion(versionString, loose);
                return IsSatisfied(version, includePrerelease: includePrerelease);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Return the set of versions that satisfy this range.
        /// </summary>
        /// <param name="versions">The versions to check.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>An IEnumerable of satisfying versions.</returns>
        public IEnumerable<SemanticVersion> Satisfying(IEnumerable<SemanticVersion> versions, bool includePrerelease = false)
        {
            return versions.Where(v => IsSatisfied(v, includePrerelease: includePrerelease));
        }

        /// <summary>
        /// Return the set of version strings that satisfy this range.
        /// Invalid version specifications are skipped.
        /// </summary>
        /// <param name="versions">The version strings to check.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>An IEnumerable of satisfying version strings.</returns>
        public IEnumerable<string> Satisfying(IEnumerable<string> versions, bool loose = false, bool includePrerelease = false)
        {
            return versions.Where(v => IsSatisfied(v, loose, includePrerelease));
        }

        /// <summary>
        /// Return the maximum version that satisfies this range.
        /// </summary>
        /// <param name="versions">The versions to select from.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>The maximum satisfying version, or null if no versions satisfied this range.</returns>
        public SemanticVersion MaxSatisfying(IEnumerable<SemanticVersion> versions, bool includePrerelease = false)
        {
            return Satisfying(versions, includePrerelease: includePrerelease).Max();
        }

        /// <summary>
        /// Return the maximum version that satisfies this range.
        /// </summary>
        /// <param name="versionStrings">The version strings to select from.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>The maximum satisfying version string, or null if no versions satisfied this range.</returns>
        public string MaxSatisfying(IEnumerable<string> versionStrings, bool loose = false, bool includePrerelease = false)
        {
            var versions = ValidVersions(versionStrings, loose);
            var maxVersion = MaxSatisfying(versions, includePrerelease: includePrerelease);
            return maxVersion == null ? null : maxVersion.ToString();
        }

        /// <summary>
        /// Calculate the intersection between two ranges.
        /// </summary>
        /// <param name="other">The Range to intersect this Range with</param>
        /// <returns>The Range intersection</returns>
        public Range Intersect(Range other)
        {
            var allIntersections = _comparatorSets.SelectMany(
                thisCs => other._comparatorSets.Select(thisCs.Intersect))
                .Where(cs => cs != null).ToList();

            if (allIntersections.Count == 0)
            {
                return new Range("<0.0.0");
            }
            return new Range(allIntersections);
        }

        /// <summary>
        /// Returns the range specification string used when constructing this range.
        /// </summary>
        /// <returns>The range string</returns>
        public override string ToString()
        {
            return _rangeSpec;
        }

        public bool Equals(Range other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            var thisSet = new HashSet<ComparatorSet>(_comparatorSets);
            return thisSet.SetEquals(other._comparatorSets);
        }

        public override bool Equals(object other)
        {
            return Equals(other as Range);
        }

        public static bool operator ==(Range a, Range b)
        {
            if (ReferenceEquals(a, null))
            {
                return ReferenceEquals(b, null);
            }
            return a.Equals(b);
        }

        public static bool operator !=(Range a, Range b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            // XOR is commutative, so this hash code is independent
            // of the order of comparators.
            return _comparatorSets.Aggregate(0, (accum, next) => accum ^ next.GetHashCode());
        }

        // Static convenience methods

        /// <summary>
        /// Determine whether the given version satisfies a given range.
        /// With an invalid version this method returns false.
        /// </summary>
        /// <param name="rangeSpec">The range specification.</param>
        /// <param name="versionString">The version to check.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>true if the range is satisfied by the version.</returns>
        public static bool IsSatisfied(string rangeSpec, string versionString, bool loose = false, bool includePrerelease = false)
        {
            var range = new Range(rangeSpec);
            return range.IsSatisfied(versionString, loose: loose, includePrerelease: includePrerelease);
        }

        /// <summary>
        /// Return the set of version strings that satisfy a given range.
        /// Invalid version specifications are skipped.
        /// </summary>
        /// <param name="rangeSpec">The range specification.</param>
        /// <param name="versions">The version strings to check.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <returns>An IEnumerable of satisfying version strings.</returns>
        public static IEnumerable<string> Satisfying(string rangeSpec, IEnumerable<string> versions, bool loose = false, bool includePrerelease = false)
        {
            var range = new Range(rangeSpec);
            return range.Satisfying(versions, loose: loose, includePrerelease: includePrerelease);
        }

        /// <summary>
        /// Return the maximum version that satisfies a given range.
        /// </summary>
        /// <param name="rangeSpec">The range specification.</param>
        /// <param name="versionStrings">The version strings to select from.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="includePrerelease">When true, allow prerelease versions to satisfy the range.</param>
        /// <returns>The maximum satisfying version string, or null if no versions satisfied this range.</returns>
        public static string MaxSatisfying(string rangeSpec, IEnumerable<string> versionStrings, bool loose = false, bool includePrerelease = false)
        {
            var range = new Range(rangeSpec);
            return range.MaxSatisfying(versionStrings, includePrerelease: includePrerelease);
        }

        /// <summary>
        /// Construct a new range from a range specification.
        /// </summary>
        /// <param name="rangeSpec">The range specification string.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <exception cref="System.ArgumentException">Thrown when the range specification is invalid.</exception>
        /// <returns>The Range</returns>
        public static Range Parse(string rangeSpec, bool loose = false)
            => new Range(rangeSpec, loose);

        /// <summary>
        /// Try to construct a new range from a range specification. 
        /// </summary>
        /// <param name="rangeSpec">The range specification string.</param>
        /// <param name="result">The Range, or null when parse fails.</param>
        /// <returns>A boolean indicating success of the parse operation.</returns>
        public static bool TryParse(string rangeSpec, out Range result)
            => TryParse(rangeSpec, loose: false, out result);

        /// <summary>
        /// Try to construct a new range from a range specification. 
        /// </summary>
        /// <param name="rangeSpec">The range specification string.</param>
        /// <param name="loose">When true, be more forgiving of some invalid version specifications.</param>
        /// <param name="result">The Range, or null when parse fails.</param>
        /// <returns>A boolean indicating success of the parse operation.</returns>
        public static bool TryParse(string rangeSpec, bool loose, out Range result)
        {
            try
            {
                result = Parse(rangeSpec, loose);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        private IEnumerable<SemanticVersion> ValidVersions(IEnumerable<string> versionStrings, bool loose)
        {
            foreach (var v in versionStrings)
            {
                SemanticVersion version = null;

                try
                {
                    version = new SemanticVersion(v, loose);
                }
                catch (ArgumentException) { } // Skip

                if (version != null)
                {
                    yield return version;
                }
            }
        }
    }
}
