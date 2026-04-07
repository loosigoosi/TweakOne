using System;
using System.Linq;
using TweakOne.IsoXml.V3;
using Xunit;

namespace TweakOne.Tests.IsoXml.V3;

public sealed class IsoXmlGuidanceRectificationServiceTests
{
    [Fact]
    public void AnalyzeRectification_WhenCurveStaysWithinTolerance_ReturnsAcceptedCandidatesForBothDirections()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9998d, 45.0002d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Curved row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000004d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999996d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000005d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Rectified row");

        Assert.Equal(2, result.Candidates.Count);
        Assert.All(result.Candidates, candidate => Assert.True(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.Standard, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Single(candidate.GeneratedLines));
        Assert.All(result.Candidates, candidate => Assert.InRange(candidate.MaxDeviationMeters, 0d, 0.10d));
        Assert.Contains(result.Candidates, candidate => candidate.SignedApplicationOffsetMeters > 0d);
        Assert.Contains(result.Candidates, candidate => candidate.SignedApplicationOffsetMeters < 0d);
        Assert.Equal(3d, result.ApplicationOffsetMeters, 3);
    }

    [Fact]
    public void AnalyzeRectification_WhenDeviationIsBorderline_ReturnsTwoPassOutputsForBothDirections()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9997d, 45.0003d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Borderline row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000012d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999988d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000013d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Two-pass row");

        Assert.Equal(2, result.Candidates.Count);
        Assert.All(result.Candidates, candidate => Assert.True(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.TwoPass, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Equal(2, candidate.GeneratedLines.Count));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.ExcessDeviationMeters > 0d));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.ExcessDeviationMeters < 0.20d));
        Assert.All(result.Candidates, candidate => Assert.Equal(result.ApplicationOffsetMeters * 2d, Math.Abs(candidate.SignedApplicationOffsetMeters), 6));
        Assert.Equal(4, result.Candidates.Sum(candidate => candidate.GeneratedLines.Count));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.GeneratedLines[0].Points.Count > 2));
        Assert.All(result.Candidates, candidate => Assert.Equal(2, candidate.GeneratedLines[1].Points.Count));

        var positiveCandidate = Assert.Single(result.Candidates.Where(candidate => candidate.SignedApplicationOffsetMeters > 0d));
        Assert.Equal(positiveCandidate.GeneratedLines[1].Points[0].East, positiveCandidate.GeneratedLines[0].Points[0].East, 6);
        Assert.Equal(positiveCandidate.GeneratedLines[1].Points[^1].East, positiveCandidate.GeneratedLines[0].Points[^1].East, 6);
    }

    [Fact]
    public void AnalyzeRectification_WhenManualOnePassIsSelected_RejectsBorderlineCandidates()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9997d, 45.0003d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Borderline row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000012d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999988d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000013d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Manual one-pass row", IsoXmlGuidanceRectificationPassSelectionMode.Manual, 1);

        Assert.All(result.Candidates, candidate => Assert.False(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.Rejected, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Empty(candidate.GeneratedLines));
    }

    [Fact]
    public void AnalyzeRectification_WhenManualTwoPassIsSelected_ForcesTwoPassOutput()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9998d, 45.0002d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Curved row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000004d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999996d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000005d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Manual two-pass row", IsoXmlGuidanceRectificationPassSelectionMode.Manual, 2);

        Assert.All(result.Candidates, candidate => Assert.True(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.TwoPass, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Equal(2, candidate.GeneratedLines.Count));
    }

    [Fact]
    public void AnalyzeRectification_WhenManualFourPassIsSelected_ForcesProgressiveOutput()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9998d, 45.0002d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Curved row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000004d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999996d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000005d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Manual four-pass row", IsoXmlGuidanceRectificationPassSelectionMode.Manual, 4);

        Assert.All(result.Candidates, candidate => Assert.True(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.MultiPass, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Equal(4, candidate.GeneratedLines.Count));
        Assert.All(result.Candidates, candidate => Assert.Equal(result.ApplicationOffsetMeters * 4d, Math.Abs(candidate.SignedApplicationOffsetMeters), 6));
        Assert.All(result.Candidates, candidate => Assert.Equal(2, candidate.GeneratedLines[^1].Points.Count));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.GeneratedLines[0].Points.Count > 2));

        var positiveCandidate = Assert.Single(result.Candidates.Where(candidate => candidate.SignedApplicationOffsetMeters > 0d));
        var smoothingDeviations = positiveCandidate.GeneratedLines
            .Take(positiveCandidate.GeneratedLines.Count - 1)
            .Select(GetMaxNormalDeviationMeters)
            .ToArray();

        Assert.Equal(3, smoothingDeviations.Length);
        Assert.True(smoothingDeviations[0] > smoothingDeviations[1]);
        Assert.True(smoothingDeviations[1] > smoothingDeviations[2]);
        Assert.InRange(smoothingDeviations[2], 0d, 0.10d);

        var firstReduction = smoothingDeviations[0] - smoothingDeviations[1];
        var secondReduction = smoothingDeviations[1] - smoothingDeviations[2];
        Assert.InRange(Math.Abs(firstReduction - secondReduction), 0d, 0.01d);
    }

    [Fact]
    public void AnalyzeRectification_WhenDeviationRequiresMoreThanTwoPasses_ReturnsAutomaticMultiPassOutput()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9997d, 45.0003d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Moderately wavy row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.0000022d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.9999978d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.0000024d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.0000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.05d, "Automatic multi-pass row");

        Assert.All(result.Candidates, candidate => Assert.True(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.MultiPass, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.InRange(candidate.GeneratedLines.Count, 3, IsoXmlGuidanceRectificationService.MaximumAutomaticPassCount));
        Assert.All(result.Candidates, candidate => Assert.Equal(2, candidate.GeneratedLines[^1].Points.Count));
    }

    [Fact]
    public void AnalyzeRectification_WhenCurveExceedsTolerance_ReturnsRejectedCandidates()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9996d, 45.0004d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Wavy row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.000050d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.999950d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.000060d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.10d, "Rejected row");

        Assert.All(result.Candidates, candidate => Assert.False(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.Rejected, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.Empty(candidate.GeneratedLines));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.MaxDeviationMeters > 0.10d));
    }

    [Fact]
    public void AnalyzeRectification_WhenAutomaticPassRequirementExceedsSupportedMaximum_BoundsRejectedPreviewOffset()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.9996d, 45.0004d, 6.9995d, 7.0015d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Very wavy row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.000050d, East = 7.000250d },
                new IsoXmlPoint { Type = 2, North = 44.999950d, East = 7.000500d },
                new IsoXmlPoint { Type = 2, North = 45.000060d, East = 7.000750d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 4, 0.01d, "Unsupported automatic row");

        Assert.All(result.Candidates, candidate => Assert.False(candidate.IsAccepted));
        Assert.All(result.Candidates, candidate => Assert.Equal(IsoXmlGuidanceRectificationMode.Rejected, candidate.Mode));
        Assert.All(result.Candidates, candidate => Assert.True(candidate.RequiredPassCount > IsoXmlGuidanceRectificationService.MaximumAutomaticPassCount));
        Assert.All(result.Candidates, candidate => Assert.Equal(result.ApplicationOffsetMeters * IsoXmlGuidanceRectificationService.MaximumAutomaticPassCount, Math.Abs(candidate.SignedApplicationOffsetMeters), 6));
    }

    [Fact]
    public void AnalyzeRectification_WhenTargetBoundaryIsNarrow_KeepsTheOriginalRectificationExtents()
    {
        var service = new IsoXmlGuidanceRectificationService();
        var target = new IsoXmlPartfield
        {
            Polygons = { CreateRectanglePolygon(44.99995d, 45.00005d, 7.00020d, 7.00080d) }
        };
        var source = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = "Straight row",
            Points =
            {
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.000000d },
                new IsoXmlPoint { Type = 2, North = 45.000000d, East = 7.001000d }
            }
        };

        var result = service.AnalyzeRectification(target, source, 0.75d, 1, 0.10d, "Boundary fitted row");

        var positiveCandidate = Assert.Single(result.Candidates.Where(candidate => candidate.SignedApplicationOffsetMeters > 0d));

        Assert.InRange(positiveCandidate.CandidateLine.Points[0].East, 6.99999d, 7.00001d);
        Assert.InRange(positiveCandidate.CandidateLine.Points[1].East, 7.00099d, 7.00101d);
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

    private static double GetMaxNormalDeviationMeters(IsoXmlLineString line)
    {
        var projectedPoints = line.Points
            .Select(static point => new ProjectedPoint(point.East, point.North))
            .ToArray();
        var centroid = new ProjectedPoint(projectedPoints.Average(static point => point.X), projectedPoints.Average(static point => point.Y));
        var direction = ComputePrincipalDirection(projectedPoints, centroid);
        var normal = new ProjectedPoint(-direction.Y, direction.X);

        return projectedPoints
            .Select(point =>
            {
                var relative = new ProjectedPoint(point.X - centroid.X, point.Y - centroid.Y);
                return Math.Abs((relative.X * normal.X) + (relative.Y * normal.Y));
            })
            .Max() * 111_319.49079327358d;
    }

    private static ProjectedPoint ComputePrincipalDirection(ProjectedPoint[] points, ProjectedPoint centroid)
    {
        var sxx = 0d;
        var syy = 0d;
        var sxy = 0d;

        foreach (var point in points)
        {
            var dx = point.X - centroid.X;
            var dy = point.Y - centroid.Y;
            sxx += dx * dx;
            syy += dy * dy;
            sxy += dx * dy;
        }

        var angle = 0.5d * Math.Atan2(2d * sxy, sxx - syy);
        return new ProjectedPoint(Math.Cos(angle), Math.Sin(angle));
    }

    private readonly record struct ProjectedPoint(double X, double Y);
}
