using System;

namespace csscript
{
    public class SemanticVersion : IComparable<SemanticVersion>
    {
        public Version Version { get; set; }
        public string Prerelease { get; set; }

        public static SemanticVersion Parse(string version)
        {
            var parts = version.Split('-');
            return new SemanticVersion
            {
                Version = new Version(parts[0]),
                Prerelease = parts.Length > 1 ? parts[1] : null
            };
        }

        // compare by version, then by prerelease (stable > prerelease)
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