using System;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlCenteredRectificationServiceTests
{
    [Fact]
    public void BuildPlan_WhenParallelCorrectionLinesAreProvided_ComputesResidualOffsetsAndSuggestedNames()
    {
        var service = new IsoXmlCenteredRectificationService();
        var partfield = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9999d, 45.0001d, 6.9999d, 7.0014d) }
        };
        var referenceLine = CreateGuidanceLine(
            "Pippo rettificata",
            (45.000000d, 7.000000d),
            (45.000000d, 7.00127041d));
        var firstPass = CreateParallelLine(referenceLine, 3.12d, "Pass 1");
        var secondPass = CreateParallelLine(referenceLine, 6.95d, "Pass 2");

        var plan = service.BuildPlan(partfield, referenceLine, new[] { firstPass, secondPass }, 3d, 9d, "Est rettifica");

        Assert.Equal(2, plan.OffsetRows.Count);
        Assert.Equal(12, plan.OffsetRows[0].OffsetACentimeters);
        Assert.Equal(12, plan.OffsetRows[0].OffsetBCentimeters);
        Assert.Equal("Est rettifica_(1)_A+12_B+12", plan.OffsetRows[0].SuggestedDesignator);
        Assert.Equal(95, plan.OffsetRows[1].OffsetACentimeters);
        Assert.Equal(95, plan.OffsetRows[1].OffsetBCentimeters);
        Assert.Equal("Est rettifica_Retta_A+95_B+95", plan.OffsetRows[1].SuggestedDesignator);
    }

    [Fact]
    public void BuildPlan_WhenReferenceExtendsBeyondFieldBoundary_PlacesMarkerCutsInsideTheTrimmedEnds()
    {
        var service = new IsoXmlCenteredRectificationService();
        var partfield = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.99995d, 45.00005d, 7.00020d, 7.00234d) }
        };
        var referenceLine = CreateGuidanceLine(
            "Pippo rettificata",
            (45.000000d, 7.000000d),
            (45.000000d, 7.00254082d));
        var correctionLine = CreateParallelLine(referenceLine, 3d, "Pass 1");

        var plan = service.BuildPlan(partfield, referenceLine, new[] { correctionLine }, 3d, 9d, "Est rettifica");
        var markerAMidpoint = Midpoint(plan.MarkerLineA);
        var markerBMidpoint = Midpoint(plan.MarkerLineB);

        Assert.Equal("Est rettifica_Retta_A+00_B+00", plan.OffsetRows[0].SuggestedDesignator);
        Assert.InRange(DistanceMeters(new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.00020d }, markerAMidpoint), 8.5d, 9.5d);
        Assert.InRange(DistanceMeters(new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.00234d }, markerBMidpoint), 8.5d, 9.5d);
    }

    [Fact]
    public void BuildPlan_WhenCorrectionLinesAreOnNegativeSide_KeepsResidualOffsetsInCentimeters()
    {
        var service = new IsoXmlCenteredRectificationService();
        var partfield = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9999d, 45.0001d, 6.9999d, 7.0014d) }
        };
        var referenceLine = CreateGuidanceLine(
            "Pippo rettificata",
            (45.000000d, 7.000000d),
            (45.000000d, 7.00127041d));
        var firstPass = CreateParallelLine(referenceLine, -3.12d, "Pass 1");
        var secondPass = CreateParallelLine(referenceLine, -6.95d, "Pass 2");

        var plan = service.BuildPlan(partfield, referenceLine, new[] { firstPass, secondPass }, 3d, 9d, "Ovest rettifica");

        Assert.Equal(-12, plan.OffsetRows[0].OffsetACentimeters);
        Assert.Equal(-12, plan.OffsetRows[0].OffsetBCentimeters);
        Assert.Equal("Ovest rettifica_(1)_A-12_B-12", plan.OffsetRows[0].SuggestedDesignator);
        Assert.Equal(-95, plan.OffsetRows[1].OffsetACentimeters);
        Assert.Equal(-95, plan.OffsetRows[1].OffsetBCentimeters);
        Assert.Equal("Ovest rettifica_Retta_A-95_B-95", plan.OffsetRows[1].SuggestedDesignator);
    }

    private static IsoXmlPolygon CreateRectanglePolygon(double south, double north, double west, double east)
    {
        return new IsoXmlPolygon
        {
            Type = 1,
            LineStrings =
            {
                new IsoXmlLineString
                {
                    Type = 1,
                    Points =
                    {
                        new IsoXmlPoint { Type = 2, North = south, East = west },
                        new IsoXmlPoint { Type = 2, North = south, East = east },
                        new IsoXmlPoint { Type = 2, North = north, East = east },
                        new IsoXmlPoint { Type = 2, North = north, East = west },
                        new IsoXmlPoint { Type = 2, North = south, East = west }
                    }
                }
            }
        };
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

    private static IsoXmlLineString CreateParallelLine(IsoXmlLineString source, double northOffsetMeters, string designator)
    {
        var northOffsetDegrees = northOffsetMeters / 111_319.49079327358d;
        return CreateGuidanceLine(
            designator,
            (source.Points[0].North + northOffsetDegrees, source.Points[0].East),
            (source.Points[^1].North + northOffsetDegrees, source.Points[^1].East));
    }

    private static IsoXmlPoint Midpoint(IsoXmlLineString line)
    {
        return new IsoXmlPoint
        {
            Type = 2,
            North = (line.Points[0].North + line.Points[^1].North) / 2d,
            East = (line.Points[0].East + line.Points[^1].East) / 2d
        };
    }

    private static double DistanceMeters(IsoXmlPoint first, IsoXmlPoint second)
    {
        var deltaNorth = (second.North - first.North) * 111_319.49079327358d;
        var cosLatitude = Math.Cos(first.North * Math.PI / 180d);
        var deltaEast = (second.East - first.East) * 111_319.49079327358d * cosLatitude;
        return Math.Sqrt((deltaNorth * deltaNorth) + (deltaEast * deltaEast));
    }
}