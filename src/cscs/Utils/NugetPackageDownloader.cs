#region Licence...

//-----------------------------------------------------------------------------
// Date:	16/02/2026
// Module:	NugetPackageDownloader.cs
// Classes:	...
//
// This module contains the definition of the utility classes used by CS-Script modules
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2004-2026 Oleg Shilo
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.IO;
using System.Linq;
using csscript;

namespace CSScripting
{
    /// <summary>
    /// Provides functionality to download the latest version of the Microsoft.Net.Sdk.Compilers.Toolset NuGet package
    /// and extract the C# compiler executable (csc.exe) from it using direct HTTP requests.
    /// </summary>
    /// <remarks>This class enables retrieval of the C# compiler without requiring any NuGet client libraries
    /// by interacting directly with the NuGet feed. It supports specifying a custom NuGet feed, destination directory,
    /// and whether to include prerelease versions. If no destination is provided, the standard NuGet cache location is
    /// used. An exception is thrown if the package or csc.exe cannot be found.</remarks>
    public class NugetPackageDownloader
    {
        /// <summary>
        /// Represents a static event handler that is invoked to report progress output messages during long-running
        /// operations.
        /// </summary>
        /// <remarks>Subscribe to this event to receive progress updates as string messages. This event is
        /// typically used to inform users about the current status of ongoing tasks. Ensure that event handlers are
        /// thread-safe if progress updates may occur from multiple threads.</remarks>
        public static Action<string> OnProgressOutput = x => { };

        /// <summary>
        /// Downloads the latest available SDK compiler package to the specified location.
        /// </summary>
        /// <remarks>If the specified destination path does not exist, it will be created. Ensure that the
        /// destination directory has write permissions before calling this method.</remarks>
        /// <param name="destination">The file path where the SDK compiler package will be downloaded. If null, the package is saved to the
        /// default download directory.</param>
        /// <param name="includePrereleases">true to include pre-release versions of the SDK compiler package in the search; otherwise, false.</param>
        /// <returns>The full file path of the downloaded SDK compiler package.</returns>
        public static string DownloadLatestSdkCompiler(string destination = null, bool includePrereleases = false)
            => DownloadLatestCompiler(Globals.SdkCompilerPackageName, destination, includePrereleases);

        /// <summary>
        /// Downloads the latest version of the framework compiler from the NuGet package repository.
        /// </summary>
        /// <remarks>This method retrieves the latest available version of the framework compiler from the
        /// NuGet package source. Ensure that the destination directory exists and is writable before calling this
        /// method.</remarks>
        /// <param name="destination">The optional file path where the downloaded compiler will be saved. If null, the default download location
        /// is used. The specified path must have write permissions.</param>
        /// <param name="includePrereleases">true to include pre-release versions of the framework compiler in the search; otherwise, false. The default
        /// is false.</param>
        /// <returns>A string containing the full path to the downloaded framework compiler.</returns>
        public static string DownloadLatestFrameworkCompiler(string destination = null, bool includePrereleases = false)
            => DownloadLatestCompiler(Globals.FrameworkToolsetPackageName, destination, includePrereleases);

        /// <summary>
        /// Downloads the latest version of the Microsoft.Net.Sdk.Compilers.Toolset package from a specified NuGet feed
        /// and extracts the csc.exe compiler executable.
        /// </summary>
        /// <remarks>This method is useful for automating the retrieval of the latest C# compiler from
        /// NuGet feeds, including custom feeds and pre-release versions. It ensures that the compiler is available
        /// locally for build or scripting scenarios.</remarks>
        /// <param name="packageName"> The name of the NuGet nuget package to be downloaded. </param>
        /// <param name="customNuGetFeed">The URL of the custom NuGet feed to use for downloading the package. If null, the default NuGet feed is
        /// used.</param>
        /// <param name="destination">The directory where the package will be downloaded and extracted. If not specified, the standard NuGet cache
        /// location is used.</param>
        /// <param name="includePrereleases">Specifies whether to include pre-release versions of the package when searching for the latest version.</param>
        /// <returns>The file path of the downloaded csc.exe compiler executable.</returns>
        /// <exception cref="ApplicationException">Thrown if the package cannot be found in the specified feed, if csc.exe is not found in the downloaded
        /// package, or if the download or extraction process fails.</exception>
        public static string DownloadLatestCompiler(string packageName, string destination = null, bool includePrereleases = false, string customNuGetFeed = null)
        {
            string packagePath = DownloadLatestPackage(packageName, destination, includePrereleases, customNuGetFeed);
            try
            {
                // Find csc.exe in the downloaded package
                string cscPath = Directory.GetFiles(packagePath, "csc.exe", SearchOption.AllDirectories)
                                          .FirstOrDefault()?
                                          .GetFullPath();

                if (cscPath == null)
                    throw new ApplicationException($"csc.exe not found in downloaded package at {packagePath}");

                OnProgressOutput($"Compiler downloaded successfully: {cscPath}");

                return cscPath;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to download {packageName}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Downloads the latest available version of a specified NuGet package to a given directory.
        /// </summary>
        /// <remarks>If the destination directory does not exist, it will be created automatically. The
        /// method provides progress output during the download process.</remarks>
        /// <param name="packageName">The name of the NuGet package to download. Cannot be null or empty.</param>
        /// <param name="destination">The directory path where the package will be downloaded. If not specified, the default NuGet cache location
        /// is used.</param>
        /// <param name="includePrereleases">Specifies whether to include pre-release versions when determining the latest package version. Set to <see
        /// langword="true"/> to include pre-releases; otherwise, only stable versions are considered.</param>
        /// <param name="customNuGetFeed">An optional custom NuGet feed URL to use for downloading the package. If not provided, the default NuGet
        /// feed is used.</param>
        /// <returns>The file path of the downloaded package.</returns>
        /// <exception cref="ApplicationException">Thrown if the package cannot be found in the specified feed or if the download fails.</exception>
        public static string DownloadLatestPackage(string packageName, string destination = null, bool includePrereleases = false, string customNuGetFeed = null)
        {
            string nugetFeed = customNuGetFeed ?? "https://api.nuget.org/v3/index.json";

            try
            {
                // If no destination specified, use standard NuGet cache
                if (destination == null)
                {
                    destination = Path.Combine(
                        Environment.SpecialFolder.UserProfile.GetPath(),
                        ".nuget",
                        "packages",
                        packageName.ToLower());
                }

                // Get the latest version info from NuGet
                string latestVersion = GetLatestPackageVersionHttp(packageName, nugetFeed, includePrereleases);

                if (latestVersion == null)
                    throw new ApplicationException($"Cannot find package '{packageName}' in feed '{nugetFeed}'");

                OnProgressOutput($"Downloading {packageName} version {latestVersion}...");

                // Download the package using HTTP
                string packagePath = Path.Combine(destination, latestVersion);

                DownloadAndExtractNuGetPackage(packageName, latestVersion, nugetFeed, packagePath);

                OnProgressOutput($"Package downloaded successfully: {packagePath}");

                return packagePath;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to download {packageName}: {ex.Message}", ex);
            }
        }

        static string GetLatestPackageVersionHttp(string packageName, string feedUrl, bool includePrerelease)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // Get service index
                var indexResponse = client.GetStringAsync(feedUrl).Result;
                var serviceIndex = System.Text.Json.JsonDocument.Parse(indexResponse);

                // Find the PackageBaseAddress resource
                string packageBaseUrl = null;
                string searchQueryUrl = null;

                foreach (var resource in serviceIndex.RootElement.GetProperty("resources").EnumerateArray())
                {
                    var type = resource.GetProperty("@type").GetString();
                    if (type == "PackageBaseAddress/3.0.0")
                    {
                        packageBaseUrl = resource.GetProperty("@id").GetString();
                    }
                    else if (type == "SearchQueryService")
                    {
                        searchQueryUrl = resource.GetProperty("@id").GetString();
                    }
                }

                // Use search query to get versions
                if (searchQueryUrl != null)
                {
                    string searchUrl = $"{searchQueryUrl}?q=packageid:{packageName}&prerelease={includePrerelease.ToString().ToLower()}&take=1";
                    var searchResponse = client.GetStringAsync(searchUrl).Result;
                    var searchResult = System.Text.Json.JsonDocument.Parse(searchResponse);

                    var data = searchResult.RootElement.GetProperty("data");
                    if (data.GetArrayLength() > 0)
                    {
                        var package = data[0];
                        return package.GetProperty("version").GetString();
                    }
                }

                // Fallback: try to get versions from package base address
                if (packageBaseUrl != null)
                {
                    string versionsUrl = $"{packageBaseUrl}{packageName.ToLower()}/index.json";
                    var versionsResponse = client.GetStringAsync(versionsUrl).Result;
                    var versionsDoc = System.Text.Json.JsonDocument.Parse(versionsResponse);

                    var versions = versionsDoc.RootElement.GetProperty("versions").EnumerateArray()
                        .Select(v => v.GetString())
                        .Select(v => SemanticVersion.Parse(v))
                        .Where(v => includePrerelease || v.Prerelease == null)
                        .OrderByDescending(v => v)
                        .ToList();

                    return versions.FirstOrDefault()?.Version.ToString();
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to query NuGet feed: {ex.Message}", ex);
            }
        }

        static void DownloadAndExtractNuGetPackage(string packageName, string version, string feedUrl, string destination)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                // Get service index to find package base address
                var indexResponse = client.GetStringAsync(feedUrl).Result;
                var serviceIndex = System.Text.Json.JsonDocument.Parse(indexResponse);

                string packageBaseUrl = null;
                foreach (var resource in serviceIndex.RootElement.GetProperty("resources").EnumerateArray())
                {
                    var type = resource.GetProperty("@type").GetString();
                    if (type == "PackageBaseAddress/3.0.0")
                    {
                        packageBaseUrl = resource.GetProperty("@id").GetString();
                        break;
                    }
                }

                if (packageBaseUrl == null)
                    throw new ApplicationException("Cannot find PackageBaseAddress in NuGet feed");

                // Construct download URL
                string downloadUrl = $"{packageBaseUrl}{packageName.ToLower()}/{version.ToLower()}/{packageName.ToLower()}.{version.ToLower()}.nupkg";

                OnProgressOutput($"Downloading from: {downloadUrl}");

                // Download .nupkg file
                var packageBytes = client.GetByteArrayAsync(downloadUrl).Result;

                // Create temp file for the package
                string tempNupkg = Path.Combine(Path.GetTempPath(), $"{packageName}.{version}.nupkg");
                File.WriteAllBytes(tempNupkg, packageBytes);

                try
                {
                    // Extract (nupkg is just a zip file)
                    Directory.CreateDirectory(destination);
#if class_lib
                    destination.DeleteDir();
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempNupkg, destination);
#else
                    System.IO.Compression.ZipFile.ExtractToDirectory(tempNupkg, destination, true);
#endif
                    OnProgressOutput($"Package extracted to: {destination}");
                }
                finally
                {
                    // Cleanup temp file
                    try { File.Delete(tempNupkg); } catch { }
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to download package: {ex.Message}", ex);
            }
        }
    }
}