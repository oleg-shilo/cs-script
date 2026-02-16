using System;

namespace csscript
{
    /// <summary>
    /// Represents a semantic version, consisting of a version number and an optional prerelease label, and provides
    /// functionality for parsing and comparing semantic versions.    /// </summary>
    /// <remarks>Semantic versions are compared first by their version number and then by their prerelease
    /// label, with stable versions considered greater than prerelease versions. This class can be used to parse
    /// semantic version strings and to determine the ordering of different semantic versions according to semantic
    /// versioning conventions.</remarks>
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        /// <summary>
        /// Gets or sets the version information for the application or component.
        /// </summary>
        /// <remarks>The version typically includes major, minor, build, and revision numbers. This
        /// property is useful for versioning, compatibility checks, and display purposes.</remarks>
        public Version Version { get; set; }

        /// <summary>
        /// Gets or sets the prerelease version identifier for the package.
        /// </summary>
        /// <remarks>This property allows the specification of a prerelease version, which can be used to
        /// indicate that the package is in a pre-release state and may not be fully stable.</remarks>
        public string Prerelease { get; set; }

        /// <summary>
        /// Parses a string that represents a semantic version and returns a corresponding SemanticVersion object.
        /// </summary>
        /// <remarks>If the input string does not include a prerelease label, the Prerelease property of
        /// the returned object will be null.</remarks>
        /// <param name="version">The string representation of the semantic version to parse. The expected format is 'major.minor.patch' with
        /// an optional '-prerelease' suffix.</param>
        /// <returns>A SemanticVersion object that contains the parsed version and optional prerelease information.</returns>
        public static SemanticVersion Parse(string version)
        {
            var parts = version.Split('-');
            return new SemanticVersion
            {
                Version = new Version(parts[0]),
                Prerelease = parts.Length > 1 ? parts[1] : null
            };
        }

        /// <summary>
        /// Compares the current SemanticVersion instance to another instance and returns an integer that indicates
        /// their relative order.
        /// </summary>
        /// <remarks>Comparison is based first on the version number. Stable versions are considered
        /// greater than pre-release versions. If both versions are pre-release, the pre-release identifiers are
        /// compared using ordinal string comparison.</remarks>
        /// <param name="other">The SemanticVersion instance to compare with the current instance. Cannot be null.</param>
        /// <returns>A value less than zero if the current instance is less than the other instance; zero if they are equal; or a
        /// value greater than zero if the current instance is greater than the other instance.</returns>
        public int CompareTo(SemanticVersion other)
        {
            if (other == null) return 1;

            var versionComparison = Version.CompareTo(other.Version);
            if (versionComparison != 0)
                return versionComparison;
            if (Prerelease == null && other.Prerelease != null)
                return 1; // stable is greater than prerelease
            if (Prerelease != null && other.Prerelease == null)
                return -1; // prerelease is less than stable
            return string.Compare(Prerelease, other.Prerelease, StringComparison.Ordinal);
        }
    }
}