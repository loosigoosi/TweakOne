using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

/// <summary>
/// Builds a centerable rectification plan that exposes residual A/B shifts and short marker lines for in-cab centering.
/// </summary>
public sealed class IsoXmlCenteredRectificationService
{
    private const double EarthRadiusMeters = 6378137d;
    private const double DefaultMarkerCutLengthMeters = 1d;
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

    /// <summary>
    /// Calculates residual A/B offsets and provisional marker lines for an accepted rectification package.
    /// </summary>
    public IsoXmlCenteredRectificationPlan BuildPlan(IsoXmlPartfield partfield, IsoXmlLineString referenceLine, IReadOnlyList<IsoXmlLineString> correctionLines, double machineWidthMeters, double markerDistanceMeters, string? baseName = null)
    {
        ArgumentNullException.ThrowIfNull(partfield);
        ArgumentNullException.ThrowIfNull(referenceLine);
        ArgumentNullException.ThrowIfNull(correctionLines);

        if (referenceLine.Points.Count < 2)
        {
            throw new InvalidOperationException(Strings.GuidanceNeedsAtLeastTwoPointsError);
        }

        if (correctionLines.Count == 0)
        {
            throw new ArgumentException("At least one correction line is required.", nameof(correctionLines));
        }

        if (machineWidthMeters <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(machineWidthMeters), Strings.RectificationOffsetPositiveError);
        }

        if (markerDistanceMeters < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(markerDistanceMeters), Strings.RectificationTolerancePositiveError);
        }

        var referenceLatitudeRadians = DegreesToRadians(referenceLine.Points.Average(static point => point.North));
        var cosLatitude = Math.Cos(referenceLatitudeRadians);
        if (Math.Abs(cosLatitude) < double.Epsilon)
        {
            throw new InvalidOperationException(Strings.TranslationNotSupportedAtPolesError);
        }

        var trimmedReferenceLine = IsoXmlGuidanceBoundaryClipper.TrimPolylineEndsToBoundary(partfield, referenceLine);
        var projectedReferencePoints = trimmedReferenceLine.Points.Select(point => Project(point, cosLatitude)).ToArray();
        var totalLength = CalculatePolylineLength(projectedReferencePoints);
        var clampedMarkerDistance = Math.Min(markerDistanceMeters, totalLength / 2d);
        var markerA = SampleAtDistance(projectedReferencePoints, clampedMarkerDistance);
        var markerB = SampleAtDistance(projectedReferencePoints, Math.Max(0d, totalLength - clampedMarkerDistance));

        var markerLineA = CreateMarkerLine(markerA, cosLatitude, Strings.CenteredRectificationMarkerALabel);
        var markerLineB = CreateMarkerLine(markerB, cosLatitude, Strings.CenteredRectificationMarkerBLabel);
        var resolvedBaseName = string.IsNullOrWhiteSpace(baseName)
            ? trimmedReferenceLine.Designator ?? Strings.GuidancePathDefaultName
            : baseName.Trim();

        var offsetRows = correctionLines
            .Select((line, index) => CreateOffsetRow(IsoXmlGuidanceBoundaryClipper.TrimPolylineEndsToBoundary(partfield, line), index + 1, correctionLines.Count, resolvedBaseName, machineWidthMeters, cosLatitude, markerA, markerB))
            .ToArray();

        return new IsoXmlCenteredRectificationPlan(
            resolvedBaseName,
            machineWidthMeters,
            clampedMarkerDistance,
            markerLineA,
            markerLineB,
            new ReadOnlyCollection<IsoXmlCenteredRectificationOffsetRow>(offsetRows));
    }

    private static IsoXmlCenteredRectificationOffsetRow CreateOffsetRow(IsoXmlLineString line, int passNumber, int totalPassCount, string baseName, double machineWidthMeters, double cosLatitude, SamplePoint markerA, SamplePoint markerB)
    {
        if (line.Points.Count < 2)
        {
            throw new InvalidOperationException(Strings.GuidanceNeedsAtLeastTwoPointsError);
        }

        var projectedCandidate = line.Points.Select(point => Project(point, cosLatitude)).ToArray();
        var offsetA = MeasureSignedNormalOffset(markerA, projectedCandidate);
        var offsetB = MeasureSignedNormalOffset(markerB, projectedCandidate);
        var signedMachineWidthOffset = Math.Sign(offsetA + offsetB) * machineWidthMeters * passNumber;
        if (Math.Abs(signedMachineWidthOffset) <= double.Epsilon)
        {
            signedMachineWidthOffset = machineWidthMeters * passNumber;
        }

        var residualA = offsetA - signedMachineWidthOffset;
        var residualB = offsetB - signedMachineWidthOffset;
        var offsetACentimeters = (int)Math.Round(residualA * 100d, MidpointRounding.AwayFromZero);
        var offsetBCentimeters = (int)Math.Round(residualB * 100d, MidpointRounding.AwayFromZero);

        return new IsoXmlCenteredRectificationOffsetRow(
            passNumber,
            line.Designator ?? Strings.GuidancePathDefaultName,
            offsetACentimeters,
            offsetBCentimeters,
            CreateSuggestedDesignator(baseName, passNumber, totalPassCount, offsetACentimeters, offsetBCentimeters));
    }

    private static string CreateSuggestedDesignator(string baseName, int passNumber, int totalPassCount, int offsetACentimeters, int offsetBCentimeters)
    {
        return passNumber == totalPassCount
            ? $"{baseName}_Retta_A{FormatSignedOffset(offsetACentimeters)}_B{FormatSignedOffset(offsetBCentimeters)}"
            : $"{baseName}_({passNumber})_A{FormatSignedOffset(offsetACentimeters)}_B{FormatSignedOffset(offsetBCentimeters)}";
    }

    private static IsoXmlLineString CreateMarkerLine(SamplePoint marker, double cosLatitude, string designator)
    {
        var halfLength = DefaultMarkerCutLengthMeters / 2d;
        var start = new ProjectedPoint(
            marker.Point.X - (marker.Normal.X * halfLength),
            marker.Point.Y - (marker.Normal.Y * halfLength));
        var end = new ProjectedPoint(
            marker.Point.X + (marker.Normal.X * halfLength),
            marker.Point.Y + (marker.Normal.Y * halfLength));

        return new IsoXmlLineString
        {
            Type = 7,
            Designator = designator,
            Width = 10,
            Length = 0,
            Points =
            {
                ToIsoPoint(start, cosLatitude),
                ToIsoPoint(end, cosLatitude)
            }
        };
    }

    private static SamplePoint SampleAtDistance(IReadOnlyList<ProjectedPoint> points, double distanceFromStart)
    {
        if (points.Count < 2)
        {
            throw new InvalidOperationException(Strings.GuidanceNeedsAtLeastTwoPointsError);
        }

        var remainingDistance = distanceFromStart;
        for (var index = 0; index < points.Count - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var segmentVector = new ProjectedPoint(end.X - start.X, end.Y - start.Y);
            var segmentLength = Math.Sqrt((segmentVector.X * segmentVector.X) + (segmentVector.Y * segmentVector.Y));
            if (segmentLength <= double.Epsilon)
            {
                continue;
            }

            if (remainingDistance <= segmentLength || index == points.Count - 2)
            {
                var ratio = Math.Clamp(remainingDistance / segmentLength, 0d, 1d);
                var tangent = new ProjectedPoint(segmentVector.X / segmentLength, segmentVector.Y / segmentLength);
                var point = new ProjectedPoint(start.X + (segmentVector.X * ratio), start.Y + (segmentVector.Y * ratio));
                return new SamplePoint(point, tangent, new ProjectedPoint(-tangent.Y, tangent.X));
            }

            remainingDistance -= segmentLength;
        }

        var fallbackStart = points[^2];
        var fallbackEnd = points[^1];
        var fallbackVector = new ProjectedPoint(fallbackEnd.X - fallbackStart.X, fallbackEnd.Y - fallbackStart.Y);
        var fallbackLength = Math.Sqrt((fallbackVector.X * fallbackVector.X) + (fallbackVector.Y * fallbackVector.Y));
        if (fallbackLength <= double.Epsilon)
        {
            throw new InvalidOperationException(Strings.TranslationRequiresDistinctPointsError);
        }

        var fallbackTangent = new ProjectedPoint(fallbackVector.X / fallbackLength, fallbackVector.Y / fallbackLength);
        return new SamplePoint(fallbackEnd, fallbackTangent, new ProjectedPoint(-fallbackTangent.Y, fallbackTangent.X));
    }

    private static double MeasureSignedNormalOffset(SamplePoint samplePoint, IReadOnlyList<ProjectedPoint> candidatePoints)
    {
        var closestIntersection = FindNormalIntersection(samplePoint, candidatePoints);
        if (closestIntersection.HasValue)
        {
            return closestIntersection.Value;
        }

        var nearestPoint = FindNearestPointOnPolyline(samplePoint.Point, candidatePoints);
        var offset = new ProjectedPoint(nearestPoint.X - samplePoint.Point.X, nearestPoint.Y - samplePoint.Point.Y);
        return Dot(offset, samplePoint.Normal);
    }

    private static double? FindNormalIntersection(SamplePoint samplePoint, IReadOnlyList<ProjectedPoint> candidatePoints)
    {
        double? bestOffset = null;
        for (var index = 0; index < candidatePoints.Count - 1; index++)
        {
            var segmentStart = candidatePoints[index];
            var segmentEnd = candidatePoints[index + 1];
            var segment = new ProjectedPoint(segmentEnd.X - segmentStart.X, segmentEnd.Y - segmentStart.Y);
            var determinant = Cross(samplePoint.Normal, segment);
            if (Math.Abs(determinant) <= 1e-9)
            {
                continue;
            }

            var delta = new ProjectedPoint(segmentStart.X - samplePoint.Point.X, segmentStart.Y - samplePoint.Point.Y);
            var u = Cross(delta, segment) / determinant;
            var v = Cross(delta, samplePoint.Normal) / determinant;
            if (v < -1e-9 || v > 1d + 1e-9)
            {
                continue;
            }

            if (!bestOffset.HasValue || Math.Abs(u) < Math.Abs(bestOffset.Value))
            {
                bestOffset = u;
            }
        }

        return bestOffset;
    }

    private static ProjectedPoint FindNearestPointOnPolyline(ProjectedPoint point, IReadOnlyList<ProjectedPoint> candidatePoints)
    {
        var bestPoint = candidatePoints[0];
        var bestDistanceSquared = double.MaxValue;

        for (var index = 0; index < candidatePoints.Count - 1; index++)
        {
            var segmentStart = candidatePoints[index];
            var segmentEnd = candidatePoints[index + 1];
            var candidatePoint = FindNearestPointOnSegment(point, segmentStart, segmentEnd);
            var distanceSquared = DistanceSquared(point, candidatePoint);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestPoint = candidatePoint;
            }
        }

        return bestPoint;
    }

    private static ProjectedPoint FindNearestPointOnSegment(ProjectedPoint point, ProjectedPoint segmentStart, ProjectedPoint segmentEnd)
    {
        var segment = new ProjectedPoint(segmentEnd.X - segmentStart.X, segmentEnd.Y - segmentStart.Y);
        var segmentLengthSquared = (segment.X * segment.X) + (segment.Y * segment.Y);
        if (segmentLengthSquared <= double.Epsilon)
        {
            return segmentStart;
        }

        var relative = new ProjectedPoint(point.X - segmentStart.X, point.Y - segmentStart.Y);
        var projection = Math.Clamp(Dot(relative, segment) / segmentLengthSquared, 0d, 1d);
        return new ProjectedPoint(segmentStart.X + (segment.X * projection), segmentStart.Y + (segment.Y * projection));
    }

    private static double CalculatePolylineLength(IReadOnlyList<ProjectedPoint> points)
    {
        var total = 0d;
        for (var index = 0; index < points.Count - 1; index++)
        {
            total += Math.Sqrt(DistanceSquared(points[index], points[index + 1]));
        }

        return total;
    }

    private static string FormatSignedOffset(int centimeters)
    {
        var sign = centimeters >= 0 ? "+" : "-";
        return sign + Math.Abs(centimeters).ToString("00", CultureInfo.InvariantCulture);
    }

    private static ProjectedPoint Project(IsoXmlPoint point, double cosLatitude)
    {
        return new ProjectedPoint(
            DegreesToRadians(point.East) * EarthRadiusMeters * cosLatitude,
            DegreesToRadians(point.North) * EarthRadiusMeters);
    }

    private static IsoXmlPoint ToIsoPoint(ProjectedPoint projectedPoint, double cosLatitude)
    {
        return new IsoXmlPoint
        {
            Type = 2,
            North = RadiansToDegrees(projectedPoint.Y / EarthRadiusMeters),
            East = RadiansToDegrees(projectedPoint.X / (EarthRadiusMeters * cosLatitude))
        };
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180d;

    private static double RadiansToDegrees(double value) => value * 180d / Math.PI;

    private static double DistanceSquared(ProjectedPoint first, ProjectedPoint second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double Dot(ProjectedPoint first, ProjectedPoint second) => (first.X * second.X) + (first.Y * second.Y);

    private static double Cross(ProjectedPoint first, ProjectedPoint second) => (first.X * second.Y) - (first.Y * second.X);

    private readonly record struct ProjectedPoint(double X, double Y);

    private readonly record struct SamplePoint(ProjectedPoint Point, ProjectedPoint Tangent, ProjectedPoint Normal);
}

public sealed record IsoXmlCenteredRectificationPlan(
    string BaseName,
    double MachineWidthMeters,
    double MarkerDistanceMeters,
    IsoXmlLineString MarkerLineA,
    IsoXmlLineString MarkerLineB,
    IReadOnlyList<IsoXmlCenteredRectificationOffsetRow> OffsetRows);

public sealed record IsoXmlCenteredRectificationOffsetRow(
    int PassNumber,
    string OriginalDesignator,
    int OffsetACentimeters,
    int OffsetBCentimeters,
    string SuggestedDesignator);