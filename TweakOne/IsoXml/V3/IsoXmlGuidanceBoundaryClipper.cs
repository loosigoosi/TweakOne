using System;
using System.Collections.Generic;
using System.Linq;

namespace TweakOne.IsoXml.V3;

internal static class IsoXmlGuidanceBoundaryClipper
{
    private const double EarthRadiusMeters = 6378137d;

    public static IsoXmlLineString FitInfiniteSegmentToBoundary(IsoXmlPartfield partfield, IsoXmlLineString lineString)
    {
        ArgumentNullException.ThrowIfNull(partfield);
        ArgumentNullException.ThrowIfNull(lineString);

        var clipped = lineString.DeepClone();
        if (clipped.Points.Count != 2)
        {
            return clipped;
        }

        var boundaryLineStrings = GetBoundaryLineStrings(partfield);
        if (boundaryLineStrings.Count == 0)
        {
            return clipped;
        }

        var firstPoint = clipped.Points[0];
        var lastPoint = clipped.Points[1];
        var referenceLatitudeRadians = DegreesToRadians((firstPoint.North + lastPoint.North) / 2d);
        var cosLatitude = Math.Cos(referenceLatitudeRadians);
        if (Math.Abs(cosLatitude) < double.Epsilon)
        {
            return clipped;
        }

        var projectedFirst = Project(firstPoint, cosLatitude);
        var projectedLast = Project(lastPoint, cosLatitude);
        var direction = new ProjectedPoint(projectedLast.X - projectedFirst.X, projectedLast.Y - projectedFirst.Y);
        var directionLength = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        if (directionLength <= double.Epsilon)
        {
            return clipped;
        }

        var intersections = new List<LineIntersection>();
        foreach (var boundaryLineString in boundaryLineStrings)
        {
            for (var index = 0; index < boundaryLineString.Points.Count - 1; index++)
            {
                var segmentStart = Project(boundaryLineString.Points[index], cosLatitude);
                var segmentEnd = Project(boundaryLineString.Points[index + 1], cosLatitude);
                if (TryIntersectInfiniteLineWithSegment(projectedFirst, direction, segmentStart, segmentEnd, out var intersection)
                    && !intersections.Any(existing => AreSamePoint(existing.Point, intersection.Point)))
                {
                    intersections.Add(intersection);
                }
            }
        }

        if (intersections.Count < 2)
        {
            return clipped;
        }

        var ordered = intersections.OrderBy(static intersection => intersection.LineParameter).ToArray();
        SetProjectedPoint(clipped.Points[0], ordered[0].Point, cosLatitude);
        SetProjectedPoint(clipped.Points[1], ordered[^1].Point, cosLatitude);
        return clipped;
    }

    public static IsoXmlLineString TrimPolylineEndsToBoundary(IsoXmlPartfield partfield, IsoXmlLineString lineString)
    {
        ArgumentNullException.ThrowIfNull(partfield);
        ArgumentNullException.ThrowIfNull(lineString);

        var clipped = lineString.DeepClone();
        if (clipped.Points.Count < 2)
        {
            return clipped;
        }

        if (clipped.Points.Count == 2)
        {
            return FitInfiniteSegmentToBoundary(partfield, clipped);
        }

        var boundaryLineStrings = GetBoundaryLineStrings(partfield);
        if (boundaryLineStrings.Count == 0)
        {
            return clipped;
        }

        var boundaryPoints = GetBoundaryPoints(partfield);
        if (boundaryPoints.Count == 0)
        {
            return clipped;
        }

        var referenceLatitudeRadians = DegreesToRadians(clipped.Points.Average(static point => point.North));
        var cosLatitude = Math.Cos(referenceLatitudeRadians);
        if (Math.Abs(cosLatitude) < double.Epsilon)
        {
            return clipped;
        }

        var projectedPoints = clipped.Points.Select(point => Project(point, cosLatitude)).ToArray();
        var cumulativeLengths = new double[projectedPoints.Length];
        for (var index = 1; index < projectedPoints.Length; index++)
        {
            cumulativeLengths[index] = cumulativeLengths[index - 1] + Distance(projectedPoints[index - 1], projectedPoints[index]);
        }

        var samples = new List<PathSample>();
        for (var index = 0; index < projectedPoints.Length; index++)
        {
            if (IsInsideTargetBoundary(clipped.Points[index], boundaryPoints))
            {
                samples.Add(new PathSample(cumulativeLengths[index], projectedPoints[index]));
            }
        }

        for (var index = 0; index < projectedPoints.Length - 1; index++)
        {
            var segmentStart = projectedPoints[index];
            var segmentEnd = projectedPoints[index + 1];
            var segmentLength = Distance(segmentStart, segmentEnd);
            if (segmentLength <= double.Epsilon)
            {
                continue;
            }

            var intersections = new List<SegmentIntersection>();
            foreach (var boundaryLineString in boundaryLineStrings)
            {
                for (var boundaryIndex = 0; boundaryIndex < boundaryLineString.Points.Count - 1; boundaryIndex++)
                {
                    var boundarySegmentStart = Project(boundaryLineString.Points[boundaryIndex], cosLatitude);
                    var boundarySegmentEnd = Project(boundaryLineString.Points[boundaryIndex + 1], cosLatitude);
                    if (TryIntersectSegmentWithSegment(segmentStart, segmentEnd, boundarySegmentStart, boundarySegmentEnd, out var intersection)
                        && !intersections.Any(existing => AreSamePoint(existing.Point, intersection.Point)))
                    {
                        intersections.Add(intersection);
                    }
                }
            }

            foreach (var intersection in intersections.OrderBy(static item => item.SegmentParameter))
            {
                samples.Add(new PathSample(
                    cumulativeLengths[index] + (segmentLength * intersection.SegmentParameter),
                    intersection.Point));
            }
        }

        if (samples.Count < 2)
        {
            return clipped;
        }

        var orderedSamples = samples.OrderBy(static sample => sample.PathPosition).ToArray();
        var start = orderedSamples[0];
        var end = orderedSamples[^1];
        if (end.PathPosition - start.PathPosition <= 0.01d)
        {
            return clipped;
        }

        var trimmedPoints = new List<ProjectedPoint> { start.Point };
        for (var index = 0; index < projectedPoints.Length; index++)
        {
            var position = cumulativeLengths[index];
            if (position > start.PathPosition + 0.01d && position < end.PathPosition - 0.01d)
            {
                trimmedPoints.Add(projectedPoints[index]);
            }
        }

        if (!AreSamePoint(trimmedPoints[^1], end.Point))
        {
            trimmedPoints.Add(end.Point);
        }

        if (trimmedPoints.Count < 2)
        {
            return clipped;
        }

        clipped.Points.Clear();
        foreach (var point in trimmedPoints)
        {
            clipped.Points.Add(ToIsoPoint(point, cosLatitude));
        }

        return clipped;
    }

    private static List<IsoXmlPoint> GetBoundaryPoints(IsoXmlPartfield partfield)
    {
        return partfield.Polygons
            .Where(static polygon => polygon.Type == 1)
            .SelectMany(static polygon => polygon.LineStrings)
            .Where(static lineString => lineString.Points.Count >= 3)
            .SelectMany(static lineString => lineString.Points)
            .ToList();
    }

    private static List<IsoXmlLineString> GetBoundaryLineStrings(IsoXmlPartfield partfield)
    {
        return partfield.Polygons
            .Where(static polygon => polygon.Type == 1)
            .SelectMany(static polygon => polygon.LineStrings)
            .Where(static lineString => lineString.Points.Count >= 2)
            .ToList();
    }

    private static bool IsInsideTargetBoundary(IsoXmlPoint point, IReadOnlyList<IsoXmlPoint> boundaryPoints)
    {
        var isInside = false;
        for (var index = 0; index < boundaryPoints.Count; index++)
        {
            var current = boundaryPoints[index];
            var next = boundaryPoints[(index + 1) % boundaryPoints.Count];
            var intersects = ((current.North > point.North) != (next.North > point.North))
                && (point.East < ((next.East - current.East) * (point.North - current.North) / (next.North - current.North + double.Epsilon)) + current.East);

            if (intersects)
            {
                isInside = !isInside;
            }
        }

        return isInside;
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

    private static void SetProjectedPoint(IsoXmlPoint point, ProjectedPoint projectedPoint, double cosLatitude)
    {
        point.North = RadiansToDegrees(projectedPoint.Y / EarthRadiusMeters);
        point.East = RadiansToDegrees(projectedPoint.X / (EarthRadiusMeters * cosLatitude));
    }

    private static bool TryIntersectInfiniteLineWithSegment(ProjectedPoint lineOrigin, ProjectedPoint lineDirection, ProjectedPoint segmentStart, ProjectedPoint segmentEnd, out LineIntersection intersection)
    {
        var segmentDirection = new ProjectedPoint(segmentEnd.X - segmentStart.X, segmentEnd.Y - segmentStart.Y);
        var denominator = Cross(lineDirection, segmentDirection);
        if (Math.Abs(denominator) <= double.Epsilon)
        {
            intersection = default;
            return false;
        }

        var offset = new ProjectedPoint(segmentStart.X - lineOrigin.X, segmentStart.Y - lineOrigin.Y);
        var lineParameter = Cross(offset, segmentDirection) / denominator;
        var segmentParameter = Cross(offset, lineDirection) / denominator;
        if (segmentParameter < -1e-9 || segmentParameter > 1d + 1e-9)
        {
            intersection = default;
            return false;
        }

        intersection = new LineIntersection(
            lineParameter,
            new ProjectedPoint(
                lineOrigin.X + (lineParameter * lineDirection.X),
                lineOrigin.Y + (lineParameter * lineDirection.Y)));
        return true;
    }

    private static bool TryIntersectSegmentWithSegment(ProjectedPoint firstStart, ProjectedPoint firstEnd, ProjectedPoint secondStart, ProjectedPoint secondEnd, out SegmentIntersection intersection)
    {
        var firstDirection = new ProjectedPoint(firstEnd.X - firstStart.X, firstEnd.Y - firstStart.Y);
        var secondDirection = new ProjectedPoint(secondEnd.X - secondStart.X, secondEnd.Y - secondStart.Y);
        var denominator = Cross(firstDirection, secondDirection);
        if (Math.Abs(denominator) <= double.Epsilon)
        {
            intersection = default;
            return false;
        }

        var offset = new ProjectedPoint(secondStart.X - firstStart.X, secondStart.Y - firstStart.Y);
        var firstParameter = Cross(offset, secondDirection) / denominator;
        var secondParameter = Cross(offset, firstDirection) / denominator;
        if (firstParameter < -1e-9 || firstParameter > 1d + 1e-9 || secondParameter < -1e-9 || secondParameter > 1d + 1e-9)
        {
            intersection = default;
            return false;
        }

        intersection = new SegmentIntersection(
            firstParameter,
            new ProjectedPoint(
                firstStart.X + (firstParameter * firstDirection.X),
                firstStart.Y + (firstParameter * firstDirection.Y)));
        return true;
    }

    private static bool AreSamePoint(ProjectedPoint first, ProjectedPoint second)
    {
        return Math.Abs(first.X - second.X) <= 0.01d && Math.Abs(first.Y - second.Y) <= 0.01d;
    }

    private static double Distance(ProjectedPoint first, ProjectedPoint second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private static double Cross(ProjectedPoint first, ProjectedPoint second) => (first.X * second.Y) - (first.Y * second.X);

    private static double DegreesToRadians(double value) => value * Math.PI / 180d;

    private static double RadiansToDegrees(double value) => value * 180d / Math.PI;

    private readonly record struct PathSample(double PathPosition, ProjectedPoint Point);

    private readonly record struct LineIntersection(double LineParameter, ProjectedPoint Point);

    private readonly record struct SegmentIntersection(double SegmentParameter, ProjectedPoint Point);

    private readonly record struct ProjectedPoint(double X, double Y);
}
