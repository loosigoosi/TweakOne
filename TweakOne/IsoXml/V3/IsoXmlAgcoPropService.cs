using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;

namespace TweakOne.IsoXml.V3;

public sealed class IsoXmlAgcoPropService
{
    /// <summary>
    /// Ensures that injected guidance groups are represented in the proprietary AGCO metadata file.
    /// </summary>
    public void UpsertGuidanceGroups(string agcoPropFilePath, string linkListFilePath, IReadOnlyList<IsoXmlGuidanceGroup> guidanceGroups)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agcoPropFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(linkListFilePath);
        ArgumentNullException.ThrowIfNull(guidanceGroups);

        if (guidanceGroups.Count == 0)
        {
            return;
        }

        var directory = Path.GetDirectoryName(agcoPropFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var root = File.Exists(agcoPropFilePath)
            ? JsonNode.Parse(File.ReadAllText(agcoPropFilePath)) as JsonObject ?? CreateEmptyRoot()
            : CreateEmptyRoot();

        var ggps = root["ggps"] as JsonArray ?? new JsonArray();
        root["ggps"] = ggps;

        var existingIds = new HashSet<string>(
            ggps.OfType<JsonObject>()
                .Select(static entry => entry["id"]?.GetValue<string>())
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Select(static id => id!),
            StringComparer.Ordinal);

        var linkMap = LoadLinkMap(linkListFilePath);
        foreach (var guidanceGroup in guidanceGroups)
        {
            if (string.IsNullOrWhiteSpace(guidanceGroup.Id) || !existingIds.Add(guidanceGroup.Id))
            {
                continue;
            }

            if (!linkMap.TryGetValue(guidanceGroup.Id, out var uuid))
            {
                throw new InvalidOperationException($"LINKLIST mapping for guidance group '{guidanceGroup.Id}' was not found.");
            }

            ggps.Add(new JsonObject
            {
                ["id"] = guidanceGroup.Id,
                ["uuid"] = uuid,
                ["type"] = 0,
                ["source"] = 2,
                ["gd_wayline_version"] = 2
            });
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        File.WriteAllText(agcoPropFilePath, root.ToJsonString(options));
    }

    private static Dictionary<string, string> LoadLinkMap(string linkListFilePath)
    {
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(linkListFilePath);

        return xmlDocument.SelectNodes("/ISO11783LinkList/LGP/LNK")!
            .OfType<XmlElement>()
            .Where(static element => !string.IsNullOrWhiteSpace(element.GetAttribute("A")) && !string.IsNullOrWhiteSpace(element.GetAttribute("B")))
            .ToDictionary(static element => element.GetAttribute("A"), static element => element.GetAttribute("B"), StringComparer.Ordinal);
    }

    private static JsonObject CreateEmptyRoot()
    {
        return new JsonObject
        {
            ["VersionMajor"] = 1,
            ["VersionMinor"] = 0,
            ["pdts"] = null,
            ["wkrs"] = null,
            ["hlis"] = null,
            ["plns"] = null,
            ["ggps"] = new JsonArray()
        };
    }
}
