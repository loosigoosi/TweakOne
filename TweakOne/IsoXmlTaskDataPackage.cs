using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne;

internal sealed class IsoXmlTaskDataPackage
{
    public IsoXmlTaskDataPackage(string packagePath, string extractionRootPath, string taskDataDirectoryPath, string taskDataXmlPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(extractionRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskDataDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskDataXmlPath);

        PackagePath = packagePath;
        ExtractionRootPath = extractionRootPath;
        TaskDataDirectoryPath = taskDataDirectoryPath;
        TaskDataXmlPath = taskDataXmlPath;
    }

    public string PackagePath { get; }

    public string ExtractionRootPath { get; }

    public string TaskDataDirectoryPath { get; }

    public string TaskDataXmlPath { get; }
}

internal static class IsoXmlTaskDataPackageService
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

    public static IsoXmlTaskDataPackage Open(string packagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException(Strings.FormatPackageNotFound(packagePath), packagePath);
        }

        var extractionRootPath = Path.Combine(Path.GetTempPath(), "TweakOne", "Packages", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(extractionRootPath);
        ZipFile.ExtractToDirectory(packagePath, extractionRootPath);

        var taskDataXmlPath = Directory
            .EnumerateFiles(extractionRootPath, "TASKDATA.XML", SearchOption.AllDirectories)
            .SingleOrDefault(static path => string.Equals(Path.GetFileName(Path.GetDirectoryName(path)), "TASKDATA", StringComparison.OrdinalIgnoreCase));

        if (taskDataXmlPath is null)
        {
            throw new InvalidOperationException(Strings.FormatPackageMissingTaskDataXml(packagePath));
        }

        var taskDataDirectoryPath = Path.GetDirectoryName(taskDataXmlPath)
            ?? throw new InvalidOperationException(Strings.FormatPackageInvalidTaskDataPath(packagePath));

        return new IsoXmlTaskDataPackage(packagePath, extractionRootPath, taskDataDirectoryPath, taskDataXmlPath);
    }

    public static void SaveAs(IsoXmlTaskDataPackage package, string destinationPackagePath)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPackagePath);

        var destinationDirectory = Path.GetDirectoryName(destinationPackagePath);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (File.Exists(destinationPackagePath))
        {
            File.Delete(destinationPackagePath);
        }

        ZipFile.CreateFromDirectory(package.ExtractionRootPath, destinationPackagePath, CompressionLevel.Optimal, includeBaseDirectory: false);
    }
}
