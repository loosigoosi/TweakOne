using System;
using System.Collections.Generic;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3
{
    public sealed class IsoXmlGuidancePathGenerator
    {
        private const double EarthRadiusMeters = 6378137d;
        private const int MaximumDesignatorLength = 32;
        private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

        /// <summary>
        /// Creates a deep copy of a guidance path and adds it to the target partfield.
        /// </summary>
        public IsoXmlLineString CreateSimpleCopy(IsoXmlPartfield targetPartfield, IsoXmlLineString sourceLineString, string? designator = null)
        {
            ArgumentNullException.ThrowIfNull(targetPartfield);
            ArgumentNullException.ThrowIfNull(sourceLineString);

            EnsureGuidancePath(sourceLineString);

            var copy = sourceLineString.DeepClone();
            if (!string.IsNullOrWhiteSpace(designator))
            {
                copy.Designator = designator;
            }

            targetPartfield.LineStrings.Add(copy);
            return copy;
        }

        /// <summary>
        /// Creates an experimental grouped rectification export using a single `GGP` with multiple nested `GPN` patterns.
        /// </summary>
        public IsoXmlGuidanceGroup CreateGroupedRectification(IsoXmlPartfield targetPartfield, IReadOnlyList<IsoXmlLineString> sourceLineStrings, string? designator = null)
        {
            ArgumentNullException.ThrowIfNull(targetPartfield);
            ArgumentNullException.ThrowIfNull(sourceLineStrings);

            if (sourceLineStrings.Count == 0)
            {
                throw new ArgumentException("At least one guidance line is required.", nameof(sourceLineStrings));
            }

            var existingIds = new HashSet<string>(EnumerateGuidanceIds(targetPartfield), StringComparer.Ordinal);
            var guidanceGroup = new IsoXmlGuidanceGroup
            {
                Id = GetNextGuidanceId(existingIds, "GGP"),
                Designator = NormalizeDesignator(string.IsNullOrWhiteSpace(designator) ? sourceLineStrings[^1].Designator : designator)
            };

            for (var index = 0; index < sourceLineStrings.Count; index++)
            {
                var sourceLineString = sourceLineStrings[index] ?? throw new ArgumentException("Guidance lines cannot contain null entries.", nameof(sourceLineStrings));
                EnsureGuidancePath(sourceLineString);

                var guidancePattern = new IsoXmlGuidancePattern
                {
                    Id = GetNextGuidanceId(existingIds, "GPN"),
                    Designator = NormalizeDesignator(sourceLineString.Designator),
                    Type = index == (sourceLineStrings.Count - 1)
                        ? IsoXmlGuidancePattern.AbGuidancePatternType
                        : IsoXmlGuidancePattern.CurveGuidancePatternType,
                    LineString = CreateGroupedGuidanceLineString(sourceLineString, NormalizeDesignator(sourceLineString.Designator))
                };

                guidanceGroup.GuidancePatterns.Add(guidancePattern);
            }

            targetPartfield.GuidanceGroups.Add(guidanceGroup);
            return guidanceGroup;
        }

        /// <summary>
        /// Creates a parallel translated copy of a guidance path and adds it to the target partfield.
        /// </summary>
        public IsoXmlLineString CreateTranslatedCopy(IsoXmlPartfield targetPartfield, IsoXmlLineString sourceLineString, double lateralOffsetMeters, string? designator = null)
        {
            ArgumentNullException.ThrowIfNull(targetPartfield);
            ArgumentNullException.ThrowIfNull(sourceLineString);

            EnsureGuidancePath(sourceLineString);

            if (sourceLineString.Points.Count < 2)
            {
                throw new InvalidOperationException(Strings.GuidanceNeedsAtLeastTwoPointsError);
            }

            var copy = CreateBestTranslatedCandidate(targetPartfield, sourceLineString, lateralOffsetMeters);
            if (!string.IsNullOrWhiteSpace(designator))
            {
                copy.Designator = designator;
            }

            targetPartfield.LineStrings.Add(copy);
            return copy;
        }

        private static void EnsureGuidancePath(IsoXmlLineString sourceLineString)
        {
            if (sourceLineString.Type != IsoXmlLineString.GuidancePathType)
            {
                throw new InvalidOperationException(Strings.FormatOnlyGuidancePathTypeError(IsoXmlLineString.GuidancePathType));
            }
        }

        private static IEnumerable<string> EnumerateGuidanceIds(IsoXmlPartfield targetPartfield)
        {
            return targetPartfield.GuidanceGroups
                .Select(static group => group.Id)
                .Concat(targetPartfield.GuidanceGroups.SelectMany(static group => group.GuidancePatterns.Select(pattern => pattern.Id)))
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Select(static id => id!);
        }

        private static IsoXmlLineString CreateGroupedGuidanceLineString(IsoXmlLineString sourceLineString, string? designator)
        {
            var groupedLineString = sourceLineString.DeepClone();
            groupedLineString.Designator = designator;

            for (var index = 0; index < groupedLineString.Points.Count; index++)
            {
                groupedLineString.Points[index].Type = index switch
                {
                    0 => 6,
                    _ when index == (groupedLineString.Points.Count - 1) => 7,
                    _ => 9
                };
            }

            return groupedLineString;
        }

        private static string GetNextGuidanceId(ISet<string> existingIds, string prefix)
        {
            for (var index = 1; ; index++)
            {
                var id = $"{prefix}{index}";
                if (existingIds.Add(id))
                {
                    return id;
                }
            }
        }

        private static string? NormalizeDesignator(string? designator)
        {
            if (string.IsNullOrWhiteSpace(designator))
            {
                return null;
            }

            var trimmed = designator.Trim();
            return trimmed.Length <= MaximumDesignatorLength
                ? trimmed
                : trimmed[..MaximumDesignatorLength];
        }

        private static IsoXmlLineString CreateBestTranslatedCandidate(IsoXmlPartfield targetPartfield, IsoXmlLineString sourceLineString, double lateralOffsetMeters)
        {
            var positiveCandidate = sourceLineString.DeepClone();
            TranslatePointsInParallel(positiveCandidate, lateralOffsetMeters);

            if (Math.Abs(lateralOffsetMeters) <= double.Epsilon)
            {
                AdjustAbSegmentAlongDirectionIfNeeded(targetPartfield, positiveCandidate);
                return positiveCandidate;
            }

            var negativeCandidate = sourceLineString.DeepClone();
            TranslatePointsInParallel(negativeCandidate, -lateralOffsetMeters);

            var positiveScore = EvaluateCandidate(targetPartfield, positiveCandidate);
            var negativeScore = EvaluateCandidate(targetPartfield, negativeCandidate);
            var bestCandidate = IsBetter(negativeScore, positiveScore) ? negativeCandidate : positiveCandidate;

            AdjustAbSegmentAlongDirectionIfNeeded(targetPartfield, bestCandidate);
            return bestCandidate;
        }

        private static CandidateScore EvaluateCandidate(IsoXmlPartfield targetPartfield, IsoXmlLineString candidate)
        {
            var boundaryPoints = GetBoundaryPoints(targetPartfield);
            if (boundaryPoints.Count == 0)
            {
                return CandidateScore.Empty;
            }

            var sampledPoints = SampleCandidatePoints(candidate);
            var insideCount = sampledPoints.Count(point => IsInsideTargetBoundary(point, boundaryPoints));
            var centroid = CalculateCentroid(boundaryPoints);
            var midpoint = CalculateMidpoint(candidate.Points[0], candidate.Points[^1]);
            var centroidDistance = CalculateDistanceSquared(midpoint, centroid);

            return new CandidateScore(insideCount, centroidDistance);
        }

        private static bool IsBetter(CandidateScore candidate, CandidateScore baseline)
        {
            if (candidate.InsideCount != baseline.InsideCount)
            {
                return candidate.InsideCount > baseline.InsideCount;
            }

            return candidate.CentroidDistanceSquared < baseline.CentroidDistanceSquared;
        }

        private static void AdjustAbSegmentAlongDirectionIfNeeded(IsoXmlPartfield targetPartfield, IsoXmlLineString candidate)
        {
            if (candidate.Points.Count != 2)
            {
                return;
            }

            var fittedCandidate = IsoXmlGuidanceBoundaryClipper.FitInfiniteSegmentToBoundary(targetPartfield, candidate);
            if (!AreSamePoint(candidate.Points[0], fittedCandidate.Points[0]) || !AreSamePoint(candidate.Points[1], fittedCandidate.Points[1]))
            {
                candidate.Points[0].North = fittedCandidate.Points[0].North;
                candidate.Points[0].East = fittedCandidate.Points[0].East;
                candidate.Points[1].North = fittedCandidate.Points[1].North;
                candidate.Points[1].East = fittedCandidate.Points[1].East;
                return;
            }

            var boundaryPoints = GetBoundaryPoints(targetPartfield);
            if (boundaryPoints.Count == 0)
            {
                return;
            }

            var sampledPoints = SampleCandidatePoints(candidate);
            if (sampledPoints.Any(point => IsInsideTargetBoundary(point, boundaryPoints)))
            {
                return;
            }

            var centroid = CalculateCentroid(boundaryPoints);
            TranslatePointsAlongDirection(candidate, centroid);
        }

        private static void TranslatePointsInParallel(IsoXmlLineString lineString, double lateralOffsetMeters)
        {
            var firstPoint = lineString.Points[0];
            var lastPoint = lineString.Points[^1];

            var referenceLatitudeRadians = DegreesToRadians((firstPoint.North + lastPoint.North) / 2d);
            var cosLatitude = Math.Cos(referenceLatitudeRadians);
            if (Math.Abs(cosLatitude) < double.Epsilon)
            {
                throw new InvalidOperationException(Strings.TranslationNotSupportedAtPolesError);
            }

            var firstProjected = Project(firstPoint, cosLatitude);
            var lastProjected = Project(lastPoint, cosLatitude);
            var directionX = lastProjected.X - firstProjected.X;
            var directionY = lastProjected.Y - firstProjected.Y;
            var directionLength = Math.Sqrt((directionX * directionX) + (directionY * directionY));

            if (directionLength <= double.Epsilon)
            {
                throw new InvalidOperationException(Strings.TranslationRequiresDistinctPointsError);
            }

            var normalX = -directionY / directionLength;
            var normalY = directionX / directionLength;
            var offsetX = normalX * lateralOffsetMeters;
            var offsetY = normalY * lateralOffsetMeters;

            foreach (var point in lineString.Points)
            {
                var projected = Project(point, cosLatitude);
                var translated = new ProjectedPoint(projected.X + offsetX, projected.Y + offsetY);
                point.North = RadiansToDegrees(translated.Y / EarthRadiusMeters);
                point.East = RadiansToDegrees(translated.X / (EarthRadiusMeters * cosLatitude));
            }
        }

        private static void TranslatePointsAlongDirection(IsoXmlLineString lineString, IsoXmlPoint targetPoint)
        {
            var firstPoint = lineString.Points[0];
            var lastPoint = lineString.Points[^1];

            var referenceLatitudeRadians = DegreesToRadians((firstPoint.North + lastPoint.North + targetPoint.North) / 3d);
            var cosLatitude = Math.Cos(referenceLatitudeRadians);
            if (Math.Abs(cosLatitude) < double.Epsilon)
            {
                throw new InvalidOperationException(Strings.TranslationNotSupportedAtPolesError);
            }

            var firstProjected = Project(firstPoint, cosLatitude);
            var lastProjected = Project(lastPoint, cosLatitude);
            var targetProjected = Project(targetPoint, cosLatitude);
            var directionX = lastProjected.X - firstProjected.X;
            var directionY = lastProjected.Y - firstProjected.Y;
            var directionLength = Math.Sqrt((directionX * directionX) + (directionY * directionY));

            if (directionLength <= double.Epsilon)
            {
                throw new InvalidOperationException(Strings.TranslationRequiresDistinctPointsError);
            }

            var unitX = directionX / directionLength;
            var unitY = directionY / directionLength;
            var midpoint = new ProjectedPoint((firstProjected.X + lastProjected.X) / 2d, (firstProjected.Y + lastProjected.Y) / 2d);
            var targetProjectionDistance = ((targetProjected.X - firstProjected.X) * unitX) + ((targetProjected.Y - firstProjected.Y) * unitY);
            var projectedTargetOnLine = new ProjectedPoint(firstProjected.X + (targetProjectionDistance * unitX), firstProjected.Y + (targetProjectionDistance * unitY));
            var shiftX = projectedTargetOnLine.X - midpoint.X;
            var shiftY = projectedTargetOnLine.Y - midpoint.Y;

            foreach (var point in lineString.Points)
            {
                var projected = Project(point, cosLatitude);
                var translated = new ProjectedPoint(projected.X + shiftX, projected.Y + shiftY);
                point.North = RadiansToDegrees(translated.Y / EarthRadiusMeters);
                point.East = RadiansToDegrees(translated.X / (EarthRadiusMeters * cosLatitude));
            }
        }

        private static IReadOnlyList<IsoXmlPoint> SampleCandidatePoints(IsoXmlLineString candidate)
        {
            if (candidate.Points.Count == 2)
            {
                return new[]
                {
                    candidate.Points[0],
                    CalculateMidpoint(candidate.Points[0], candidate.Points[1]),
                    candidate.Points[1]
                };
            }

            return candidate.Points;
        }

        private static List<IsoXmlPoint> GetBoundaryPoints(IsoXmlPartfield targetPartfield)
        {
            return targetPartfield.Polygons
                .Where(static polygon => polygon.Type == 1)
                .SelectMany(static polygon => polygon.LineStrings)
                .Where(static lineString => lineString.Points.Count >= 3)
                .SelectMany(static lineString => lineString.Points)
                .ToList();
        }

        private static bool IsInsideTargetBoundary(IsoXmlPoint point, IReadOnlyList<IsoXmlPoint> boundaryPoints)
        {
            var isInside = false;
            for (var i = 0; i < boundaryPoints.Count; i++)
            {
                var current = boundaryPoints[i];
                var next = boundaryPoints[(i + 1) % boundaryPoints.Count];

                var intersects = ((current.North > point.North) != (next.North > point.North))
                    && (point.East < ((next.East - current.East) * (point.North - current.North) / (next.North - current.North + double.Epsilon)) + current.East);

                if (intersects)
                {
                    isInside = !isInside;
                }
            }

            return isInside;
        }

        private static IsoXmlPoint CalculateCentroid(IReadOnlyList<IsoXmlPoint> boundaryPoints)
        {
            return new IsoXmlPoint
            {
                North = boundaryPoints.Average(static point => point.North),
                East = boundaryPoints.Average(static point => point.East)
            };
        }

        private static IsoXmlPoint CalculateMidpoint(IsoXmlPoint firstPoint, IsoXmlPoint lastPoint)
        {
            return new IsoXmlPoint
            {
                North = (firstPoint.North + lastPoint.North) / 2d,
                East = (firstPoint.East + lastPoint.East) / 2d
            };
        }

        private static double CalculateDistanceSquared(IsoXmlPoint firstPoint, IsoXmlPoint secondPoint)
        {
            var deltaNorth = firstPoint.North - secondPoint.North;
            var deltaEast = firstPoint.East - secondPoint.East;
            return (deltaNorth * deltaNorth) + (deltaEast * deltaEast);
        }

        private static ProjectedPoint Project(IsoXmlPoint point, double cosLatitude)
        {
            return new ProjectedPoint(
                DegreesToRadians(point.East) * EarthRadiusMeters * cosLatitude,
                DegreesToRadians(point.North) * EarthRadiusMeters);
        }

        private static bool AreSamePoint(IsoXmlPoint first, IsoXmlPoint second)
        {
            return Math.Abs(first.North - second.North) <= 1e-9 && Math.Abs(first.East - second.East) <= 1e-9;
        }

        private static double Cross(ProjectedPoint first, ProjectedPoint second)
        {
            return (first.X * second.Y) - (first.Y * second.X);
        }

        private static double DegreesToRadians(double value) => value * Math.PI / 180d;

        private static double RadiansToDegrees(double value) => value * 180d / Math.PI;

        private readonly record struct CandidateScore(int InsideCount, double CentroidDistanceSquared)
        {
            public static CandidateScore Empty { get; } = new(0, double.MaxValue);
        }

        private readonly record struct ProjectedPoint(double X, double Y);
    }
}
