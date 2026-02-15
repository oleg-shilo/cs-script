using System;
using System.Linq;
using csscript;
using CSScripting;
using CSScriptLib;
using static CSScriptLib.CSharpParser;
using Xunit;

namespace Misc
{
    public class GenericTests
    {
        [Fact]
        public void Replace_polyfil()
        {
            Assert.Equal("bbb123bbb456bbb7890bbb", "aa123aa456AA7890AA".Replace("aa", "bbb", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("123bbb456bbb7890", "123aa456AA7890".Replace("aa", "bbb", StringComparison.OrdinalIgnoreCase));
            Assert.Equal("123bbb456AA7890", "123aa456AA7890".Replace("aa", "bbb", StringComparison.Ordinal));
        }
    }

    public class SemanticVersionTests
    {
        #region SemanticVersion Tests

        [Fact]
        public void SemanticVersion_Parse_StableVersion()
        {
            var version = SemanticVersion.Parse("4.11.0");

            Assert.NotNull(version);
            Assert.Equal(new Version("4.11.0"), version.Version);
            Assert.Null(version.Prerelease);
        }

        [Fact]
        public void SemanticVersion_Parse_PrereleaseVersion()
        {
            var version = SemanticVersion.Parse("4.11.0-3.24324.1");

            Assert.NotNull(version);
            Assert.Equal(new Version("4.11.0"), version.Version);
            Assert.Equal("3.24324.1", version.Prerelease);
        }

        [Fact]
        public void SemanticVersion_Parse_PrereleaseWithLabel()
        {
            var version = SemanticVersion.Parse("4.12.0-preview.1");

            Assert.NotNull(version);
            Assert.Equal(new Version("4.12.0"), version.Version);
            Assert.Equal("preview.1", version.Prerelease);
        }

        [Fact]
        public void SemanticVersion_Parse_ComplexPrerelease()
        {
            var version = SemanticVersion.Parse("5.0.0-rc.2.21505.57");

            Assert.NotNull(version);
            Assert.Equal(new Version("5.0.0"), version.Version);
            Assert.Equal("rc.2.21505.57", version.Prerelease);
        }

        [Fact]
        public void SemanticVersion_CompareTo_DifferentMajorVersions()
        {
            var v1 = SemanticVersion.Parse("4.0.0");
            var v2 = SemanticVersion.Parse("5.0.0");

            Assert.True(v2.CompareTo(v1) > 0);
            Assert.True(v1.CompareTo(v2) < 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_DifferentMinorVersions()
        {
            var v1 = SemanticVersion.Parse("4.10.0");
            var v2 = SemanticVersion.Parse("4.11.0");

            Assert.True(v2.CompareTo(v1) > 0);
            Assert.True(v1.CompareTo(v2) < 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_DifferentBuildVersions()
        {
            var v1 = SemanticVersion.Parse("4.11.0");
            var v2 = SemanticVersion.Parse("4.11.1");

            Assert.True(v2.CompareTo(v1) > 0);
            Assert.True(v1.CompareTo(v2) < 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_StableVsPrerelease()
        {
            var stable = SemanticVersion.Parse("4.11.0");
            var prerelease = SemanticVersion.Parse("4.11.0-preview.1");

            // Stable should be greater than prerelease for same version
            Assert.True(stable.CompareTo(prerelease) > 0);
            Assert.True(prerelease.CompareTo(stable) < 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_SameStableVersions()
        {
            var v1 = SemanticVersion.Parse("4.11.0");
            var v2 = SemanticVersion.Parse("4.11.0");

            Assert.Equal(0, v1.CompareTo(v2));
        }

        [Fact]
        public void SemanticVersion_CompareTo_DifferentPrereleases()
        {
            var v1 = SemanticVersion.Parse("4.11.0-alpha.1");
            var v2 = SemanticVersion.Parse("4.11.0-beta.1");

            // Should compare alphabetically
            Assert.True(v1.CompareTo(v2) < 0);
            Assert.True(v2.CompareTo(v1) > 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_PrereleaseNumbers()
        {
            var v1 = SemanticVersion.Parse("4.11.0-3.24324.1");
            var v2 = SemanticVersion.Parse("4.11.0-3.24325.1");

            Assert.True(v1.CompareTo(v2) < 0);
            Assert.True(v2.CompareTo(v1) > 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_Null()
        {
            var version = SemanticVersion.Parse("4.11.0");

            Assert.True(version.CompareTo(null) > 0);
        }

        [Fact]
        public void SemanticVersion_CompareTo_ComplexScenario()
        {
            var versions = new[]
            {
                SemanticVersion.Parse("4.10.0"),
                SemanticVersion.Parse("4.11.0-preview.1"),
                SemanticVersion.Parse("4.11.0-preview.2"),
                SemanticVersion.Parse("4.11.0"),
                SemanticVersion.Parse("4.11.1-alpha"),
                SemanticVersion.Parse("4.11.1"),
                SemanticVersion.Parse("5.0.0-rc.1"),
                SemanticVersion.Parse("5.0.0")
            };

            // Verify proper ordering
            for (int i = 0; i < versions.Length - 1; i++)
            {
                Assert.True(versions[i].CompareTo(versions[i + 1]) < 0,
                    $"Expected {versions[i].Version}-{versions[i].Prerelease ?? "stable"} < {versions[i + 1].Version}-{versions[i + 1].Prerelease ?? "stable"}");
            }
        }

        [Fact]
        public void SemanticVersion_LinqOrdering()
        {
            var versions = new[]
            {
                SemanticVersion.Parse("4.11.0"),
                SemanticVersion.Parse("4.10.0"),
                SemanticVersion.Parse("5.0.0-preview.1"),
                SemanticVersion.Parse("4.11.0-beta.1"),
                SemanticVersion.Parse("5.0.0"),
                SemanticVersion.Parse("4.11.0-alpha.1")
            };

            var sorted = versions.OrderByDescending(v => v).ToArray();

            // Expected order (descending): 5.0.0, 5.0.0-preview.1, 4.11.0, 4.11.0-beta.1, 4.11.0-alpha.1, 4.10.0
            Assert.Equal("5.0.0", sorted[0].Version.ToString());
            Assert.Null(sorted[0].Prerelease);

            Assert.Equal("5.0.0", sorted[1].Version.ToString());
            Assert.Equal("preview.1", sorted[1].Prerelease);

            Assert.Equal("4.11.0", sorted[2].Version.ToString());
            Assert.Null(sorted[2].Prerelease);

            Assert.Equal("4.11.0", sorted[3].Version.ToString());
            Assert.Equal("beta.1", sorted[3].Prerelease);

            Assert.Equal("4.11.0", sorted[4].Version.ToString());
            Assert.Equal("alpha.1", sorted[4].Prerelease);

            Assert.Equal("4.10.0", sorted[5].Version.ToString());
            Assert.Null(sorted[5].Prerelease);
        }

        [Fact]
        public void SemanticVersion_Parse_InvalidVersion_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => SemanticVersion.Parse("invalid"));
            Assert.Throws<ArgumentException>(() => SemanticVersion.Parse("4.11.x"));
        }

        #endregion SemanticVersion Tests
    }
}