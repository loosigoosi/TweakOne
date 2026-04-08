using System;
using System.Linq;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlCenteredRectificationTemplateInjectionServiceTests
{
    [Fact]
    public void Inject_WhenMatchingPartfieldExists_AddsMarkerLinesAndSuggestedGuidanceLinesToThatField()
    {
        var service = new IsoXmlCenteredRectificationTemplateInjectionService();
        var templateDocument = new IsoXmlTaskDataDocument
        {
            Partfields =
            {
                new IsoXmlPartfield { Id = "PFD1", Designator = "Other" },
                new IsoXmlPartfield { Id = "PFD2", Designator = "TEST" }
            }
        };
        var correctionLines = new[]
        {
            CreateGuidanceLine("Pass 1", (45.000000d, 7.000000d), (45.000000d, 7.001000d)),
            CreateGuidanceLine("Pass 2", (45.000030d, 7.000000d), (45.000030d, 7.001000d))
        };
        var plan = new IsoXmlCenteredRectificationPlan(
            "Est rettifica",
            3d,
            9d,
            CreateMarkerLine("Taglietto A", (45.000005d, 7.000100d), (45.000015d, 7.000100d)),
            CreateMarkerLine("Taglietto B", (45.000005d, 7.000900d), (45.000015d, 7.000900d)),
            new[]
            {
                new IsoXmlCenteredRectificationOffsetRow(1, "Pass 1", 12, 12, "Est rettifica(1)_A+12_B+12"),
                new IsoXmlCenteredRectificationOffsetRow(2, "Pass 2", 95, 95, "Est rettifica(2)_A+95_B+95")
            });

        var result = service.Inject(templateDocument, "PFD2", "TEST", plan, correctionLines);

        Assert.Equal("PFD2", result.TargetPartfield.Id);
        Assert.Equal(2, result.InjectedGuidanceLineCount);
        Assert.Equal(2, result.InjectedMarkerLineCount);
        Assert.Collection(
            templateDocument.Partfields[1].LineStrings.Select(static line => line.Designator),
            designator => Assert.Equal("Taglietto A", designator),
            designator => Assert.Equal("Taglietto B", designator),
            designator => Assert.Equal("Est rettifica(1)_A+12_B+12", designator),
            designator => Assert.Equal("Est rettifica(2)_A+95_B+95", designator));
        Assert.Empty(templateDocument.Partfields[0].LineStrings);
    }

    [Fact]
    public void Inject_WhenTemplateHasNoPartfield_ThrowsInvalidOperationException()
    {
        var service = new IsoXmlCenteredRectificationTemplateInjectionService();
        var templateDocument = new IsoXmlTaskDataDocument();
        var plan = new IsoXmlCenteredRectificationPlan(
            "Est rettifica",
            3d,
            9d,
            CreateMarkerLine("Taglietto A", (45.000005d, 7.000100d), (45.000015d, 7.000100d)),
            CreateMarkerLine("Taglietto B", (45.000005d, 7.000900d), (45.000015d, 7.000900d)),
            new[]
            {
                new IsoXmlCenteredRectificationOffsetRow(1, "Pass 1", 12, 12, "Est rettifica(1)_A+12_B+12")
            });
        var correctionLines = new[]
        {
            CreateGuidanceLine("Pass 1", (45.000000d, 7.000000d), (45.000000d, 7.001000d))
        };

        Assert.Throws<InvalidOperationException>(() => service.Inject(templateDocument, "PFD2", "TEST", plan, correctionLines));
    }

    private static IsoXmlLineString CreateGuidanceLine(string designator, params (double North, double East)[] points)
    {
        var line = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = designator
        };

        foreach (var point in points)
        {
            line.Points.Add(new IsoXmlPoint
            {
                Type = 2,
                North = point.North,
                East = point.East
            });
        }

        return line;
    }

    private static IsoXmlLineString CreateMarkerLine(string designator, params (double North, double East)[] points)
    {
        var line = new IsoXmlLineString
        {
            Type = 7,
            Designator = designator,
            Width = 10,
            Length = 0
        };

        foreach (var point in points)
        {
            line.Points.Add(new IsoXmlPoint
            {
                Type = 2,
                North = point.North,
                East = point.East
            });
        }

        return line;
    }
}
