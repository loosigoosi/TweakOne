using System;
using System.Collections.Generic;
using System.Linq;
using TweakOne.Localization;

namespace TweakOne.IsoXml.V3;

/// <summary>
/// Analyzes whether a curved guidance line can be rectified into a straight parallel candidate within an offset tolerance envelope.
/// </summary>
public sealed class IsoXmlGuidanceRectificationService
{
    private const double EarthRadiusMeters = 6378137d;
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;
    private readonly IsoXmlGuidancePathGenerator _generator = new();

    /// <summary>
    /// Builds and evaluates straight rectified guidance line candidates for both offset directions.
    /// </summary>
    public IsoXmlGuidanceRectificationResult AnalyzeRectification(IsoXmlPartfield targetPartfield, IsoXmlLineString sourceLineString, double rowSpacingMeters, int rowCount, double toleranceMeters, string? designator = null)
    {
        ArgumentNullException.ThrowIfNull(targetPartfield);
        ArgumentNullException.ThrowIfNull(sourceLineString);

        if (sourceLineString.Type != IsoXmlLineString.GuidancePathType)
        {
            throw new InvalidOperationException(Strings.FormatOnlyGuidancePathTypeError(IsoXmlLineString.GuidancePathType));
        }

        if (sourceLineString.Points.Count < 2)
        {
            throw new InvalidOperationException(Strings.GuidanceNeedsAtLeastTwoPointsError);
        }

        if (rowSpacingMeters <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(rowSpacingMeters), Strings.RectificationOffsetPositiveError);
        }

        if (rowCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), Strings.RectificationOffsetPositiveError);
        }

        if (toleranceMeters < 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(toleranceMeters), Strings.RectificationTolerancePositiveError);
        }

        var referenceLatitudeRadians = DegreesToRadians(sourceLineString.Points.Average(static point => point.North));
        var cosLatitude = Math.Cos(referenceLatitudeRadians);
        if (Math.Abs(cosLatitude) < double.Epsilon)
        {
            throw new InvalidOperationException(Strings.TranslationNotSupportedAtPolesError);
        }

        var projectedSourcePoints = sourceLineString.Points.Select(point => Project(point, cosLatitude)).ToArray();
        var centroid = new ProjectedPoint(projectedSourcePoints.Average(static point => point.X), projectedSourcePoints.Average(static point => point.Y));
        var direction = ComputePrincipalDirection(projectedSourcePoints, centroid);
        var applicationOffsetMeters = rowSpacingMeters * rowCount;

        var positiveCandidate = EvaluateCandidate(sourceLineString, projectedSourcePoints, centroid, direction, rowSpacingMeters, applicationOffsetMeters, toleranceMeters, cosLatitude, GetPositiveDesignator(sourceLineString, designator));
        var negativeCandidate = EvaluateCandidate(sourceLineString, projectedSourcePoints, centroid, direction, -rowSpacingMeters, -applicationOffsetMeters, toleranceMeters, cosLatitude, GetNegativeDesignator(sourceLineString, designator));

        return new IsoXmlGuidanceRectificationResult(rowSpacingMeters, rowCount, applicationOffsetMeters, toleranceMeters, new[] { positiveCandidate.ToResult(), negativeCandidate.ToResult() });
    }

    private RectificationCandidateEvaluation EvaluateCandidate(
        IsoXmlLineString sourceLineString,
        IReadOnlyList<ProjectedPoint> projectedSourcePoints,
        ProjectedPoint centroid,
        ProjectedPoint direction,
        double signedAnalysisOffsetMeters,
        double signedApplicationOffsetMeters,
        double toleranceMeters,
        double cosLatitude,
        string? designator)
    {
        var analysisCandidate = BuildCandidateLine(sourceLineString, projectedSourcePoints, centroid, direction, signedAnalysisOffsetMeters, cosLatitude, designator);
        var projectedAnalysisStart = Project(analysisCandidate.Points[0], cosLatitude);
        var projectedAnalysisEnd = Project(analysisCandidate.Points[1], cosLatitude);
        var analysisDirectionVector = new ProjectedPoint(projectedAnalysisEnd.X - projectedAnalysisStart.X, projectedAnalysisEnd.Y - projectedAnalysisStart.Y);

        var outputCandidate = BuildCandidateLine(sourceLineString, projectedSourcePoints, centroid, direction, signedApplicationOffsetMeters, cosLatitude, designator);

        var distances = projectedSourcePoints
            .Select(point => DistanceToInfiniteLine(point, projectedAnalysisStart, analysisDirectionVector))
            .ToArray();

        var minDistance = distances.Min();
        var maxDistance = distances.Max();
        var desiredOffset = Math.Abs(signedAnalysisOffsetMeters);
        var maxDeviation = distances.Max(distance => Math.Abs(distance - desiredOffset));
        var lowerExcess = Math.Max(0d, (desiredOffset - toleranceMeters) - minDistance);
        var upperExcess = Math.Max(0d, maxDistance - (desiredOffset + toleranceMeters));
        var excessDeviation = Math.Max(lowerExcess, upperExcess);
        var isAccepted = excessDeviation <= double.Epsilon;

        if (isAccepted)
        {
            return new RectificationCandidateEvaluation(
                outputCandidate,
                signedApplicationOffsetMeters,
                true,
                minDistance,
                maxDistance,
                maxDeviation,
                excessDeviation,
                IsoXmlGuidanceRectificationMode.Standard,
                new[] { outputCandidate });
        }

        if (excessDeviation < (toleranceMeters * 2d))
        {
            var smoothedCandidate = BuildSmoothedTransitionLine(
                sourceLineString,
                projectedSourcePoints,
                centroid,
                direction,
                signedApplicationOffsetMeters,
                cosLatitude,
                signedApplicationOffsetMeters >= 0d
                    ? GetPositiveSmoothedDesignator(sourceLineString, designator)
                    : GetNegativeSmoothedDesignator(sourceLineString, designator));

            return new RectificationCandidateEvaluation(
                outputCandidate,
                signedApplicationOffsetMeters,
                true,
                minDistance,
                maxDistance,
                maxDeviation,
                excessDeviation,
                IsoXmlGuidanceRectificationMode.TwoPass,
                new[] { smoothedCandidate, outputCandidate });
        }

        return new RectificationCandidateEvaluation(
            outputCandidate,
            signedApplicationOffsetMeters,
            false,
            minDistance,
            maxDistance,
            maxDeviation,
            excessDeviation,
            IsoXmlGuidanceRectificationMode.Rejected,
            Array.Empty<IsoXmlLineString>());
    }

    private IsoXmlLineString BuildCandidateLine(IsoXmlLineString sourceLineString, IReadOnlyList<ProjectedPoint> projectedSourcePoints, ProjectedPoint centroid, ProjectedPoint direction, double signedOffsetMeters, double cosLatitude, string? designator)
    {
        var normal = new ProjectedPoint(-direction.Y, direction.X);
        var extents = projectedSourcePoints.Select(point => Dot(Subtract(point, centroid), direction)).ToArray();
        var minExtent = extents.Min();
        var maxExtent = extents.Max();
        var offsetVector = new ProjectedPoint(normal.X * signedOffsetMeters, normal.Y * signedOffsetMeters);
        var start = Add(Add(centroid, new ProjectedPoint(direction.X * minExtent, direction.Y * minExtent)), offsetVector);
        var end = Add(Add(centroid, new ProjectedPoint(direction.X * maxExtent, direction.Y * maxExtent)), offsetVector);

        return new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator : designator,
            Points =
            {
                ToIsoPoint(start, cosLatitude),
                ToIsoPoint(end, cosLatitude)
            }
        };
    }

    private static IsoXmlLineString BuildSmoothedTransitionLine(
        IsoXmlLineString sourceLineString,
        IReadOnlyList<ProjectedPoint> projectedSourcePoints,
        ProjectedPoint centroid,
        ProjectedPoint direction,
        double signedApplicationOffsetMeters,
        double cosLatitude,
        string? designator)
    {
        var normal = new ProjectedPoint(-direction.Y, direction.X);
        var smoothedLine = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator : designator
        };

        foreach (var point in projectedSourcePoints
                     .Select(projectedPoint =>
                     {
                         var relative = Subtract(projectedPoint, centroid);
                         return new
                         {
                             Point = projectedPoint,
                             LongitudinalOffset = Dot(relative, direction),
                             LateralOffset = Dot(relative, normal)
                         };
                     })
                     .OrderBy(static entry => entry.LongitudinalOffset))
        {
            var smoothedLateralOffset = (signedApplicationOffsetMeters + point.LateralOffset) / 2d;
            var smoothedPoint = Add(
                Add(centroid, new ProjectedPoint(direction.X * point.LongitudinalOffset, direction.Y * point.LongitudinalOffset)),
                new ProjectedPoint(normal.X * smoothedLateralOffset, normal.Y * smoothedLateralOffset));
            smoothedLine.Points.Add(ToIsoPoint(smoothedPoint, cosLatitude));
        }

        return smoothedLine;
    }

    private static string GetPositiveDesignator(IsoXmlLineString sourceLineString, string? designator)
    {
        var baseDesignator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator ?? Strings.GuidancePathDefaultName : designator;
        return Strings.FormatPositiveRectifiedGuidanceDesignator(baseDesignator);
    }

    private static string GetNegativeDesignator(IsoXmlLineString sourceLineString, string? designator)
    {
        var baseDesignator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator ?? Strings.GuidancePathDefaultName : designator;
        return Strings.FormatNegativeRectifiedGuidanceDesignator(baseDesignator);
    }

    private static string GetPositiveSmoothedDesignator(IsoXmlLineString sourceLineString, string? designator)
    {
        var baseDesignator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator ?? Strings.GuidancePathDefaultName : designator;
        return Strings.FormatPositiveSmoothedGuidanceDesignator(baseDesignator);
    }

    private static string GetNegativeSmoothedDesignator(IsoXmlLineString sourceLineString, string? designator)
    {
        var baseDesignator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator ?? Strings.GuidancePathDefaultName : designator;
        return Strings.FormatNegativeSmoothedGuidanceDesignator(baseDesignator);
    }

    private static ProjectedPoint ComputePrincipalDirection(IReadOnlyList<ProjectedPoint> points, ProjectedPoint centroid)
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
        var direction = new ProjectedPoint(Math.Cos(angle), Math.Sin(angle));
        var length = Math.Sqrt((direction.X * direction.X) + (direction.Y * direction.Y));
        if (length <= double.Epsilon)
        {
            throw new InvalidOperationException(Strings.TranslationRequiresDistinctPointsError);
        }

        return new ProjectedPoint(direction.X / length, direction.Y / length);
    }

    private static double DistanceToInfiniteLine(ProjectedPoint point, ProjectedPoint lineStart, ProjectedPoint lineDirection)
    {
        var directionLength = Math.Sqrt((lineDirection.X * lineDirection.X) + (lineDirection.Y * lineDirection.Y));
        if (directionLength <= double.Epsilon)
        {
            return 0d;
        }

        var offset = Subtract(point, lineStart);
        return Math.Abs(Cross(offset, lineDirection)) / directionLength;
    }

    private static double DistanceSquared(ProjectedPoint first, ProjectedPoint second)
    {
        var dx = first.X - second.X;
        var dy = first.Y - second.Y;
        return (dx * dx) + (dy * dy);
    }

    private static double Dot(ProjectedPoint first, ProjectedPoint second) => (first.X * second.X) + (first.Y * second.Y);

    private static double Cross(ProjectedPoint first, ProjectedPoint second) => (first.X * second.Y) - (first.Y * second.X);

    private static ProjectedPoint Add(ProjectedPoint first, ProjectedPoint second) => new(first.X + second.X, first.Y + second.Y);

    private static ProjectedPoint Subtract(ProjectedPoint first, ProjectedPoint second) => new(first.X - second.X, first.Y - second.Y);

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

    private readonly record struct RectificationCandidateEvaluation(
        IsoXmlLineString CandidateLine,
        double SignedApplicationOffsetMeters,
        bool IsAccepted,
        double MinDistanceMeters,
        double MaxDistanceMeters,
        double MaxDeviationMeters,
        double ExcessDeviationMeters,
        IsoXmlGuidanceRectificationMode Mode,
        IReadOnlyList<IsoXmlLineString> GeneratedLines)
    {
        public IsoXmlGuidanceRectificationCandidateResult ToResult()
        {
            return new IsoXmlGuidanceRectificationCandidateResult(CandidateLine, SignedApplicationOffsetMeters, IsAccepted, MinDistanceMeters, MaxDistanceMeters, MaxDeviationMeters, ExcessDeviationMeters, Mode, GeneratedLines);
        }
    }

    private readonly record struct ProjectedPoint(double X, double Y);
}

/// <summary>
/// Contains the outcome of a rectification analysis for a source guidance line.
/// </summary>
public sealed class IsoXmlGuidanceRectificationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsoXmlGuidanceRectificationResult"/> class.
    /// </summary>
    public IsoXmlGuidanceRectificationResult(double rowSpacingMeters, int rowCount, double applicationOffsetMeters, double toleranceMeters, IReadOnlyList<IsoXmlGuidanceRectificationCandidateResult> candidates)
    {
        Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one rectification candidate is required.", nameof(candidates));
        }

        RowSpacingMeters = rowSpacingMeters;
        RowCount = rowCount;
        ApplicationOffsetMeters = applicationOffsetMeters;
        ToleranceMeters = toleranceMeters;
    }

    /// <summary>
    /// Gets the row spacing used to validate the rectification candidates.
    /// </summary>
    public double RowSpacingMeters { get; }

    /// <summary>
    /// Gets the number of rows used to compute the applied machine-width offset.
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    /// Gets the applied machine-width offset in meters.
    /// </summary>
    public double ApplicationOffsetMeters { get; }

    /// <summary>
    /// Gets the allowed offset tolerance in meters.
    /// </summary>
    public double ToleranceMeters { get; }

    /// <summary>
    /// Gets the generated candidates for both offset directions.
    /// </summary>
    public IReadOnlyList<IsoXmlGuidanceRectificationCandidateResult> Candidates { get; }
}

/// <summary>
/// Contains the outcome of a single rectification candidate direction.
/// </summary>
public sealed class IsoXmlGuidanceRectificationCandidateResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsoXmlGuidanceRectificationCandidateResult"/> class.
    /// </summary>
    public IsoXmlGuidanceRectificationCandidateResult(IsoXmlLineString candidateLine, double signedApplicationOffsetMeters, bool isAccepted, double minDistanceMeters, double maxDistanceMeters, double maxDeviationMeters, double excessDeviationMeters, IsoXmlGuidanceRectificationMode mode, IReadOnlyList<IsoXmlLineString> generatedLines)
    {
        CandidateLine = candidateLine ?? throw new ArgumentNullException(nameof(candidateLine));
        SignedApplicationOffsetMeters = signedApplicationOffsetMeters;
        IsAccepted = isAccepted;
        MinDistanceMeters = minDistanceMeters;
        MaxDistanceMeters = maxDistanceMeters;
        MaxDeviationMeters = maxDeviationMeters;
        ExcessDeviationMeters = excessDeviationMeters;
        Mode = mode;
        GeneratedLines = generatedLines ?? throw new ArgumentNullException(nameof(generatedLines));
    }

    /// <summary>
    /// Gets the candidate straight guidance line produced for one offset direction.
    /// </summary>
    public IsoXmlLineString CandidateLine { get; }

    /// <summary>
    /// Gets the signed applied machine-width offset in meters.
    /// </summary>
    public double SignedApplicationOffsetMeters { get; }

    /// <summary>
    /// Gets a value indicating whether the candidate line respects the offset tolerance envelope.
    /// </summary>
    public bool IsAccepted { get; }

    /// <summary>
    /// Gets the minimum measured distance from the source polyline to the analysis line in meters.
    /// </summary>
    public double MinDistanceMeters { get; }

    /// <summary>
    /// Gets the maximum measured distance from the source polyline to the analysis line in meters.
    /// </summary>
    public double MaxDistanceMeters { get; }

    /// <summary>
    /// Gets the largest absolute deviation from the desired row spacing in meters.
    /// </summary>
    public double MaxDeviationMeters { get; }

    /// <summary>
    /// Gets the amount by which the analysis exceeded the tolerance envelope in meters.
    /// </summary>
    public double ExcessDeviationMeters { get; }

    /// <summary>
    /// Gets the rectification mode chosen for this direction.
    /// </summary>
    public IsoXmlGuidanceRectificationMode Mode { get; }

    /// <summary>
    /// Gets the guidance lines to preview or apply for this direction.
    /// </summary>
    public IReadOnlyList<IsoXmlLineString> GeneratedLines { get; }
}

/// <summary>
/// Describes how a rectification candidate should be consumed.
/// </summary>
public enum IsoXmlGuidanceRectificationMode
{
    Rejected = 0,
    Standard = 1,
    TwoPass = 2
}
