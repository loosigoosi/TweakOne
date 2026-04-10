using System;
using System.IO;
using System.Linq;
using System.Xml;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlLinkListServiceTests
{
    [Fact]
    public void UpsertGuidanceLinks_WhenGroupsAreInjected_AppendsMissingGgpAndGpnMappings()
    {
        var service = new IsoXmlLinkListService();
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-LINKLIST.XML");
        File.WriteAllText(tempFilePath, """
<?xml version="1.0" encoding="UTF-8"?>
<ISO11783LinkList VersionMajor="4" VersionMinor="3">
  <LGP A="LGP-1" B="1">
    <LNK A="GGP100" B="{11111111-1111-1111-1111-111111111111}" />
  </LGP>
</ISO11783LinkList>
""");
        var taskDataDocument = new IsoXmlTaskDataDocument
        {
            VersionMajor = "4",
            VersionMinor = "3"
        };
        var guidanceGroups = new[]
        {
            new IsoXmlGuidanceGroup
            {
                Id = "GGP226",
                GuidancePatterns =
                {
                    new IsoXmlGuidancePattern { Id = "GPN226" }
                }
            },
            new IsoXmlGuidanceGroup
            {
                Id = "GGP227",
                GuidancePatterns =
                {
                    new IsoXmlGuidancePattern { Id = "GPN227" }
                }
            }
        };

        service.UpsertGuidanceLinks(tempFilePath, taskDataDocument, guidanceGroups);

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(tempFilePath);
        var links = xmlDocument.SelectNodes("/ISO11783LinkList/LGP/LNK")!.OfType<XmlElement>().ToArray();

        Assert.Contains(links, link => link.GetAttribute("A") == "GGP100");
        Assert.Contains(links, link => link.GetAttribute("A") == "GGP226");
        Assert.Contains(links, link => link.GetAttribute("A") == "GPN226");
        Assert.Contains(links, link => link.GetAttribute("A") == "GGP227");
        Assert.Contains(links, link => link.GetAttribute("A") == "GPN227");
        Assert.All(
            links.Where(link => link.GetAttribute("A") is "GGP226" or "GPN226" or "GGP227" or "GPN227"),
            link => Assert.Matches("^\\{[0-9a-fA-F-]{36}\\}$", link.GetAttribute("B")));
    }

    [Fact]
    public void UpsertGuidanceLinks_WhenLinkListDoesNotExist_CreatesNewFileWithInjectedMappings()
    {
        var service = new IsoXmlLinkListService();
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var tempFilePath = Path.Combine(tempDirectory, "LINKLIST.XML");
        var taskDataDocument = new IsoXmlTaskDataDocument
        {
            VersionMajor = "4",
            VersionMinor = "3",
            ManagementSoftwareManufacturer = "Test",
            ManagementSoftwareVersion = "1.0",
            DataTransferOrigin = "1"
        };
        var guidanceGroups = new[]
        {
            new IsoXmlGuidanceGroup
            {
                Id = "GGP226",
                GuidancePatterns =
                {
                    new IsoXmlGuidancePattern { Id = "GPN226" }
                }
            }
        };

        service.UpsertGuidanceLinks(tempFilePath, taskDataDocument, guidanceGroups);

        var xmlDocument = new XmlDocument();
        xmlDocument.Load(tempFilePath);
        Assert.Equal("4", xmlDocument.DocumentElement!.GetAttribute("VersionMajor"));
        Assert.Equal("3", xmlDocument.DocumentElement.GetAttribute("VersionMinor"));
        Assert.NotNull(xmlDocument.SelectSingleNode("/ISO11783LinkList/LGP/LNK[@A='GGP226']"));
        Assert.NotNull(xmlDocument.SelectSingleNode("/ISO11783LinkList/LGP/LNK[@A='GPN226']"));
    }
}
