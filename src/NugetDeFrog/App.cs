// ReSharper disable AccessToDisposedClosure

using System.Xml.Linq;

using Cocona;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace NugetDeFrog;

internal static class App
{
    public static int Run(
        [Argument(Description = "Path to a dependency file or a directory with files '*.deps.json'.")]
        string fileOrDirectory = ".",
        [Option("output", Description = "Path to the output project file.")]
        string outputFile = @"RuntimePackages\RuntimePackages.csproj",
        [Option("windows", Description = "Use windows target platform; required if any of the projects require windows platform")]
        bool useWindowsPlatform = false
        )
    {
        var files = GetFiles(fileOrDirectory);

        if (files.Length == 0)
        {
            Console.WriteLine($"No dependency files found in '{fileOrDirectory}'");
            return 1;
        }

        // Now load all the packages listed in the dependency files

        using var reader = new Microsoft.Extensions.DependencyModel.DependencyContextJsonReader();

        var models = files
            .Select(file => { using var stream = File.OpenRead(file); return reader.Read(stream); })
            .ToArray();

        var targetFrameworks = string.Join(";", models
            .Select(m => NuGetFramework.Parse(m.Target.Framework).GetShortFolderName())
            .Select(m => useWindowsPlatform ? $"{m}-windows" : m)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
        );

        var runtimeLibraries = models
            .SelectMany(m => m.RuntimeLibraries)
            // include only packages that provide runtime assemblies
            .Where(l => l.Type == "package")
            .Where(l => l.RuntimeAssemblyGroups.Count > 0)
            .OrderBy(l => l.Name)
            .ToArray();

        var packageIdentities = runtimeLibraries
            .Select(l => new PackageIdentity(l.Name, NuGetVersion.Parse(l.Version)))
            .Distinct()
            .OrderBy(p => p.Id)
            .ToArray();

        var multipleVersions = packageIdentities
            .GroupBy(p => p.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (multipleVersions.Length > 0)
        {
            Console.WriteLine($"Multiple versions of the following packages are referenced: {string.Join(", ", multipleVersions)}");
            return 3;
        }

        var projectNode = new XElement("Project");
        projectNode.Add(new XAttribute("Sdk", "Microsoft.NET.Sdk"));

        var propertyNode = new XElement("PropertyGroup");
        projectNode.Add(propertyNode);

        propertyNode.Add(new XElement("TargetFrameworks", targetFrameworks));
        propertyNode.Add(new XElement("ImportDirectoryBuildProps", "false"));
        propertyNode.Add(new XElement("ImportDirectoryBuildTargets", "false"));
        propertyNode.Add(new XElement("ManagePackageVersionsCentrally", "false"));

        var itemGroupNode = new XElement("ItemGroup");
        projectNode.Add(itemGroupNode);
        foreach (var packageIdentity in packageIdentities)
        {
            itemGroupNode.Add(new XElement("PackageReference",
                new XAttribute("Include", packageIdentity.Id),
                new XAttribute("Version", packageIdentity.Version.ToNormalizedString())));
        }

        var projectFile = new XDocument(projectNode);

        var folder = Path.GetDirectoryName(outputFile);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }

        projectFile.Save(outputFile);

        Console.WriteLine($"Input: {string.Join(", ", files)}");
        Console.WriteLine($"Project file saved to '{outputFile}'");

        return 0;
    }

    private static string[] GetFiles(string? fileOrDirectory)
    {
        fileOrDirectory ??= Directory.GetCurrentDirectory();

        if (Directory.Exists(fileOrDirectory))
        {
            return Directory.GetFiles(fileOrDirectory, "*.deps.json", SearchOption.TopDirectoryOnly);
        }

        if (File.Exists(fileOrDirectory))
        {
            return [fileOrDirectory];
        }

        return [];
    }
}
