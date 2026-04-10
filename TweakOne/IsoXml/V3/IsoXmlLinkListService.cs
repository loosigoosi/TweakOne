using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace TweakOne.IsoXml.V3;

public sealed class IsoXmlLinkListService
{
    /// <summary>
    /// Ensures that all injected guidance groups and patterns have LINKLIST GUID mappings in the target v4 package.
    /// </summary>
    public void UpsertGuidanceLinks(string linkListFilePath, IsoXmlTaskDataDocument taskDataDocument, IReadOnlyList<IsoXmlGuidanceGroup> guidanceGroups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(linkListFilePath);
        ArgumentNullException.ThrowIfNull(taskDataDocument);
        ArgumentNullException.ThrowIfNull(guidanceGroups);

        if (guidanceGroups.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(linkListFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var xmlDocument = new XmlDocument();
        if (File.Exists(linkListFilePath))
        {
            xmlDocument.Load(linkListFilePath);
        }
        else
        {
            CreateEmptyLinkList(xmlDocument, taskDataDocument);
        }

        var root = xmlDocument.DocumentElement ?? throw new InvalidOperationException("LINKLIST root element is missing.");
        var linkGroup = root.SelectSingleNode("LGP") as XmlElement;
        if (linkGroup is null)
        {
            linkGroup = xmlDocument.CreateElement("LGP");
            linkGroup.SetAttribute("A", "LGP-1");
            linkGroup.SetAttribute("B", "1");
            root.AppendChild(linkGroup);
        }

        var existingIds = new HashSet<string>(
            linkGroup.SelectNodes("LNK")!
                .OfType<XmlElement>()
                .Select(static element => element.GetAttribute("A"))
                .Where(static id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal);

        foreach (var guidanceGroup in guidanceGroups)
        {
            AppendLinkIfMissing(xmlDocument, linkGroup, existingIds, guidanceGroup.Id);
            foreach (var guidancePattern in guidanceGroup.GuidancePatterns)
            {
                AppendLinkIfMissing(xmlDocument, linkGroup, existingIds, guidancePattern.Id);
            }
        }

        using var writer = XmlWriter.Create(linkListFilePath, new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = NewLineHandling.Replace,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        });
        xmlDocument.Save(writer);
    }

    private static void CreateEmptyLinkList(XmlDocument xmlDocument, IsoXmlTaskDataDocument taskDataDocument)
    {
        var declaration = xmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
        xmlDocument.AppendChild(declaration);

        var root = xmlDocument.CreateElement("ISO11783LinkList");
        SetAttributeIfNotEmpty(root, "VersionMajor", taskDataDocument.VersionMajor);
        SetAttributeIfNotEmpty(root, "VersionMinor", taskDataDocument.VersionMinor);
        SetAttributeIfNotEmpty(root, "ManagementSoftwareManufacturer", taskDataDocument.ManagementSoftwareManufacturer);
        SetAttributeIfNotEmpty(root, "ManagementSoftwareVersion", taskDataDocument.ManagementSoftwareVersion);
        SetAttributeIfNotEmpty(root, "DataTransferOrigin", taskDataDocument.DataTransferOrigin);
        xmlDocument.AppendChild(root);

        var linkGroup = xmlDocument.CreateElement("LGP");
        linkGroup.SetAttribute("A", "LGP-1");
        linkGroup.SetAttribute("B", "1");
        root.AppendChild(linkGroup);
    }

    private static void AppendLinkIfMissing(XmlDocument xmlDocument, XmlElement linkGroup, ISet<string> existingIds, string? id)
    {
        if (string.IsNullOrWhiteSpace(id) || !existingIds.Add(id))
        {
            return;
        }

        var link = xmlDocument.CreateElement("LNK");
        link.SetAttribute("A", id);
        link.SetAttribute("B", $"{{{Guid.NewGuid()}}}");
        linkGroup.AppendChild(link);
    }

    private static void SetAttributeIfNotEmpty(XmlElement element, string attributeName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            element.SetAttribute(attributeName, value);
        }
    }
}
