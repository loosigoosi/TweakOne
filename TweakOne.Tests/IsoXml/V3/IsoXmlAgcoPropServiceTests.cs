using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlAgcoPropServiceTests
{
    [Fact]
    public void UpsertGuidanceGroups_WhenGroupsAreInjected_AppendsMissingAgcoEntriesUsingLinkListUuids()
    {
        var service = new IsoXmlAgcoPropService();
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var agcoPropFilePath = Path.Combine(tempDirectory, "AGCOPROP.JSN");
        var linkListFilePath = Path.Combine(tempDirectory, "LINKLIST.XML");
        File.WriteAllText(agcoPropFilePath, """
{
  "VersionMajor": 1,
  "VersionMinor": 0,
  "pdts": null,
  "wkrs": null,
  "hlis": null,
  "plns": null,
  "ggps": [
    {
      "id": "GGP100",
      "uuid": "{11111111-1111-1111-1111-111111111111}",
      "type": 0,
      "source": 0
    }
  ]
}
""");
        File.WriteAllText(linkListFilePath, """
<?xml version="1.0" encoding="UTF-8"?>
<ISO11783LinkList VersionMajor="4" VersionMinor="3">
  <LGP A="LGP-1" B="1">
    <LNK A="GGP226" B="{22222222-2222-2222-2222-222222222222}" />
    <LNK A="GGP227" B="{33333333-3333-3333-3333-333333333333}" />
  </LGP>
</ISO11783LinkList>
""");
        var guidanceGroups = new[]
        {
            new IsoXmlGuidanceGroup { Id = "GGP226" },
            new IsoXmlGuidanceGroup { Id = "GGP227" }
        };

        service.UpsertGuidanceGroups(agcoPropFilePath, linkListFilePath, guidanceGroups);

        var root = JsonNode.Parse(File.ReadAllText(agcoPropFilePath))!.AsObject();
        var ggps = root["ggps"]!.AsArray().OfType<JsonObject>().ToArray();
        Assert.Contains(ggps, entry => entry["id"]!.GetValue<string>() == "GGP100");
        Assert.Contains(ggps, entry => entry["id"]!.GetValue<string>() == "GGP226" && entry["uuid"]!.GetValue<string>() == "{22222222-2222-2222-2222-222222222222}" && entry["source"]!.GetValue<int>() == 2 && entry["gd_wayline_version"]!.GetValue<int>() == 2);
        Assert.Contains(ggps, entry => entry["id"]!.GetValue<string>() == "GGP227" && entry["uuid"]!.GetValue<string>() == "{33333333-3333-3333-3333-333333333333}" && entry["source"]!.GetValue<int>() == 2 && entry["gd_wayline_version"]!.GetValue<int>() == 2);
    }
}
