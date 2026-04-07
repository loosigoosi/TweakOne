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
    public const int MaximumAutomaticPassCount = 6;
    public const int MaximumManualPassCount = 100;

    private const double EarthRadiusMeters = 6378137d;
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;
    private readonly IsoXmlGuidancePathGenerator _generator = new();

    /// <summary>
    /// Builds and evaluates straight rectified guidance line candidates for both offset directions.
    /// </summary>
    public IsoXmlGuidanceRectificationResult AnalyzeRectification(IsoXmlPartfield targetPartfield, IsoXmlLineString sourceLineString, double rowSpacingMeters, int rowCount, double toleranceMeters, string? designator = null, IsoXmlGuidanceRectificationPassSelectionMode passSelectionMode = IsoXmlGuidanceRectificationPassSelectionMode.Automatic, int manualPassCount = 2)
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

        if (manualPassCount is < 1 or > MaximumManualPassCount)
        {
            throw new ArgumentOutOfRangeException(nameof(manualPassCount), Strings.FormatRectificationPassCountRangeError(1, MaximumManualPassCount));
        }

        var referenceLatitudeRadians = DegreesToRadians(sourceLineString.Points.Average(static point => point.North));
        var cosLatitude = Math.Cos(referenceLatitudeRadians);
        if (Math.Abs(cosLatitude) < double.Epsilon)
        {
            throw new InvalidOperationException(Strings.TranslationNotSupportedAtPolesError);
        }

        var projectedSourcePoints = sourceLineString.Points.Select(point => Project(point, cosLatitude)).ToArray();
        var applicationOffsetMeters = rowSpacingMeters * rowCount;

        var positiveCandidate = EvaluateCandidate(sourceLineString, projectedSourcePoints, applicationOffsetMeters, toleranceMeters, cosLatitude, GetPositiveDesignator(sourceLineString, designator), passSelectionMode, manualPassCount);
        var negativeCandidate = EvaluateCandidate(sourceLineString, projectedSourcePoints, -applicationOffsetMeters, toleranceMeters, cosLatitude, GetNegativeDesignator(sourceLineString, designator), passSelectionMode, manualPassCount);

        return new IsoXmlGuidanceRectificationResult(rowSpacingMeters, rowCount, applicationOffsetMeters, toleranceMeters, new[] { positiveCandidate.ToResult(), negativeCandidate.ToResult() });
    }

    private RectificationCandidateEvaluation EvaluateCandidate(
        IsoXmlLineString sourceLineString,
        IReadOnlyList<ProjectedPoint> projectedSourcePoints,
        double signedStepOffsetMeters,
        double toleranceMeters,
        double cosLatitude,
        string? designator,
        IsoXmlGuidanceRectificationPassSelectionMode passSelectionMode,
        int manualPassCount)
    {
        var initialAnalysis = AnalyzeProjectedPolyline(projectedSourcePoints);
        var maxDeviation = initialAnalysis.MaxDeviationMeters;
        var excessDeviation = Math.Max(0d, maxDeviation - toleranceMeters);
        var requiredPassCount = DetermineAutomaticPassCount(maxDeviation, toleranceMeters);

        var selectedPassCount = passSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Manual
            ? manualPassCount
            : requiredPassCount;

        var finalDesiredOffsetMeters = Math.Abs(signedStepOffsetMeters * selectedPassCount);

        if (selectedPassCount == 1)
        {
            var outputCandidate = BuildCandidateLine(sourceLineString, projectedSourcePoints, initialAnalysis.Centroid, initialAnalysis.Direction, signedStepOffsetMeters, cosLatitude, designator);
            var metrics = MeasureCandidate(projectedSourcePoints, outputCandidate, cosLatitude, finalDesiredOffsetMeters);
            return CreateStandardEvaluation(outputCandidate, signedStepOffsetMeters, requiredPassCount, metrics.MinDistanceMeters, metrics.MaxDistanceMeters, metrics.MaxDeviationMeters, excessDeviation);
        }

        return CreateProgressiveEvaluation(
            sourceLineString,
            projectedSourcePoints,
            signedStepOffsetMeters,
            toleranceMeters,
            cosLatitude,
            designator,
            maxDeviation,
            excessDeviation,
            selectedPassCount,
            requiredPassCount,
            passSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Manual ? MaximumManualPassCount : MaximumAutomaticPassCount);
    }

    private static RectificationCandidateEvaluation CreateStandardEvaluation(IsoXmlLineString outputCandidate, double signedApplicationOffsetMeters, int requiredPassCount, double minDistance, double maxDistance, double maxDeviation, double excessDeviation)
    {
        var isAccepted = requiredPassCount == 1;
        return new RectificationCandidateEvaluation(
            outputCandidate,
            signedApplicationOffsetMeters,
            requiredPassCount,
            isAccepted,
            minDistance,
            maxDistance,
            maxDeviation,
            excessDeviation,
            isAccepted ? IsoXmlGuidanceRectificationMode.Standard : IsoXmlGuidanceRectificationMode.Rejected,
            isAccepted ? new[] { outputCandidate } : Array.Empty<IsoXmlLineString>());
    }

    private static RectificationCandidateEvaluation CreateProgressiveEvaluation(IsoXmlLineString sourceLineString, IReadOnlyList<ProjectedPoint> projectedSourcePoints, double signedStepOffsetMeters, double toleranceMeters, double cosLatitude, string? designator, double maxDeviation, double excessDeviation, int passCount, int requiredPassCount, int supportedPassCountLimit)
    {
        var effectivePassCount = Math.Min(passCount, supportedPassCountLimit);
        var signedFinalOffsetMeters = signedStepOffsetMeters * effectivePassCount;
        var fallbackAnalysis = AnalyzeProjectedPolyline(projectedSourcePoints);
        var fallbackCandidate = BuildCandidateLine(sourceLineString, projectedSourcePoints, fallbackAnalysis.Centroid, fallbackAnalysis.Direction, signedFinalOffsetMeters, cosLatitude, designator);

        if (!CanUsePassCount(requiredPassCount, passCount, supportedPassCountLimit))
        {
            var fallbackMetrics = MeasureCandidate(projectedSourcePoints, fallbackCandidate, cosLatitude, Math.Abs(signedFinalOffsetMeters));
            return new RectificationCandidateEvaluation(
                fallbackCandidate,
                signedFinalOffsetMeters,
                requiredPassCount,
                false,
                fallbackMetrics.MinDistanceMeters,
                fallbackMetrics.MaxDistanceMeters,
                maxDeviation,
                excessDeviation,
                IsoXmlGuidanceRectificationMode.Rejected,
                Array.Empty<IsoXmlLineString>());
        }

        var generatedLines = new List<IsoXmlLineString>(effectivePassCount);
        var currentPoints = projectedSourcePoints.ToArray();
        for (var passIndex = 1; passIndex < effectivePassCount; passIndex++)
        {
            var currentAnalysis = AnalyzeProjectedPolyline(currentPoints);
            var remainingSmoothingPassCount = effectivePassCount - passIndex;
            var targetResidualDeviation = remainingSmoothingPassCount <= 0
                ? 0d
                : toleranceMeters + ((currentAnalysis.MaxDeviationMeters - toleranceMeters) * ((remainingSmoothingPassCount - 1d) / remainingSmoothingPassCount));
            var residualFactor = currentAnalysis.MaxDeviationMeters <= double.Epsilon
                ? 0d
                : Math.Clamp(targetResidualDeviation / currentAnalysis.MaxDeviationMeters, 0d, 1d);
            var smoothingLine = BuildProgressiveTransitionLine(
                sourceLineString,
                currentAnalysis.Entries,
                currentAnalysis.Centroid,
                currentAnalysis.Direction,
                signedStepOffsetMeters,
                residualFactor,
                cosLatitude,
                GetProgressiveDesignator(sourceLineString, designator, signedStepOffsetMeters >= 0d, passIndex, effectivePassCount));

            generatedLines.Add(smoothingLine);
            currentPoints = smoothingLine.Points.Select(point => Project(point, cosLatitude)).ToArray();
        }

        var finalAnalysis = AnalyzeProjectedPolyline(currentPoints);
        var outputCandidate = BuildCandidateLine(sourceLineString, currentPoints, finalAnalysis.Centroid, finalAnalysis.Direction, signedStepOffsetMeters, cosLatitude, designator);
        generatedLines.Add(outputCandidate);
        var finalMetrics = MeasureCandidate(projectedSourcePoints, outputCandidate, cosLatitude, Math.Abs(signedFinalOffsetMeters));

        return new RectificationCandidateEvaluation(
            outputCandidate,
            signedFinalOffsetMeters,
            requiredPassCount,
            true,
            finalMetrics.MinDistanceMeters,
            finalMetrics.MaxDistanceMeters,
            maxDeviation,
            excessDeviation,
            effectivePassCount == 2 ? IsoXmlGuidanceRectificationMode.TwoPass : IsoXmlGuidanceRectificationMode.MultiPass,
            generatedLines);
    }

    private static int DetermineAutomaticPassCount(double maxDeviation, double toleranceMeters)
    {
        if (maxDeviation <= toleranceMeters + double.Epsilon)
        {
            return 1;
        }

        if (toleranceMeters <= double.Epsilon)
        {
            return MaximumAutomaticPassCount + 1;
        }

        return Math.Max(2, (int)Math.Ceiling(maxDeviation / toleranceMeters));
    }

    private static bool CanUsePassCount(int requiredPassCount, int passCount, int supportedPassCountLimit)
    {
        if (passCount < 1 || passCount > supportedPassCountLimit)
        {
            return false;
        }

        return requiredPassCount <= passCount;
    }

    private static IsoXmlLineString BuildCandidateLine(IsoXmlLineString sourceLineString, IReadOnlyList<ProjectedPoint> projectedSourcePoints, ProjectedPoint centroid, ProjectedPoint direction, double signedOffsetMeters, double cosLatitude, string? designator)
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

    private static IsoXmlLineString BuildProgressiveTransitionLine(
        IsoXmlLineString sourceLineString,
        IReadOnlyList<ProjectedPointAnalysis> projectedPointAnalyses,
        ProjectedPoint centroid,
        ProjectedPoint direction,
        double signedApplicationOffsetMeters,
        double residualFactor,
        double cosLatitude,
        string? designator)
    {
        var smoothedLine = new IsoXmlLineString
        {
            Type = IsoXmlLineString.GuidancePathType,
            Designator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator : designator
        };

        foreach (var point in projectedPointAnalyses)
        {
            var smoothedLateralOffset = signedApplicationOffsetMeters + (point.LateralOffset * residualFactor);
            var smoothedPoint = Add(
                Add(centroid, new ProjectedPoint(direction.X * point.LongitudinalOffset, direction.Y * point.LongitudinalOffset)),
                new ProjectedPoint((-direction.Y) * smoothedLateralOffset, direction.X * smoothedLateralOffset));
            smoothedLine.Points.Add(ToIsoPoint(smoothedPoint, cosLatitude));
        }

        return smoothedLine;
    }

    private static PolylineRegressionAnalysis AnalyzeProjectedPolyline(IReadOnlyList<ProjectedPoint> projectedPoints)
    {
        var centroid = new ProjectedPoint(projectedPoints.Average(static point => point.X), projectedPoints.Average(static point => point.Y));
        var direction = ComputePrincipalDirection(projectedPoints, centroid);
        var normal = new ProjectedPoint(-direction.Y, direction.X);
        var entries = projectedPoints
            .Select(projectedPoint =>
            {
                var relative = Subtract(projectedPoint, centroid);
                return new ProjectedPointAnalysis(projectedPoint, Dot(relative, direction), Dot(relative, normal));
            })
            .OrderBy(static entry => entry.LongitudinalOffset)
            .ToArray();

        return new PolylineRegressionAnalysis(
            centroid,
            direction,
            entries,
            entries.Max(static entry => Math.Abs(entry.LateralOffset)));
    }

    private static CandidateMetrics MeasureCandidate(IReadOnlyList<ProjectedPoint> projectedSourcePoints, IsoXmlLineString candidateLine, double cosLatitude, double desiredOffsetMeters)
    {
        var projectedAnalysisStart = Project(candidateLine.Points[0], cosLatitude);
        var projectedAnalysisEnd = Project(candidateLine.Points[1], cosLatitude);
        var analysisDirectionVector = new ProjectedPoint(projectedAnalysisEnd.X - projectedAnalysisStart.X, projectedAnalysisEnd.Y - projectedAnalysisStart.Y);
        var distances = projectedSourcePoints
            .Select(point => DistanceToInfiniteLine(point, projectedAnalysisStart, analysisDirectionVector))
            .ToArray();

        return new CandidateMetrics(
            distances.Min(),
            distances.Max(),
            distances.Max(distance => Math.Abs(distance - desiredOffsetMeters)));
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

    private static string GetProgressiveDesignator(IsoXmlLineString sourceLineString, string? designator, bool usesPositiveDirection, int passIndex, int passCount)
    {
        var baseDesignator = string.IsNullOrWhiteSpace(designator) ? sourceLineString.Designator ?? Strings.GuidancePathDefaultName : designator;
        return usesPositiveDirection
            ? Strings.FormatPositiveProgressiveGuidanceDesignator(baseDesignator, passIndex, passCount)
            : Strings.FormatNegativeProgressiveGuidanceDesignator(baseDesignator, passIndex, passCount);
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
        int RequiredPassCount,
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
            return new IsoXmlGuidanceRectificationCandidateResult(CandidateLine, SignedApplicationOffsetMeters, RequiredPassCount, IsAccepted, MinDistanceMeters, MaxDistanceMeters, MaxDeviationMeters, ExcessDeviationMeters, Mode, GeneratedLines);
        }
    }

    private readonly record struct CandidateMetrics(double MinDistanceMeters, double MaxDistanceMeters, double MaxDeviationMeters);

    private readonly record struct ProjectedPointAnalysis(ProjectedPoint Point, double LongitudinalOffset, double LateralOffset);

    private readonly record struct PolylineRegressionAnalysis(ProjectedPoint Centroid, ProjectedPoint Direction, IReadOnlyList<ProjectedPointAnalysis> Entries, double MaxDeviationMeters);

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
    public IsoXmlGuidanceRectificationCandidateResult(IsoXmlLineString candidateLine, double signedApplicationOffsetMeters, int requiredPassCount, bool isAccepted, double minDistanceMeters, double maxDistanceMeters, double maxDeviationMeters, double excessDeviationMeters, IsoXmlGuidanceRectificationMode mode, IReadOnlyList<IsoXmlLineString> generatedLines)
    {
        CandidateLine = candidateLine ?? throw new ArgumentNullException(nameof(candidateLine));
        SignedApplicationOffsetMeters = signedApplicationOffsetMeters;
        RequiredPassCount = requiredPassCount;
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
    /// Gets the number of passes required by the current rectification criterion for this direction.
    /// </summary>
    public int RequiredPassCount { get; }

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
    TwoPass = 2,
    MultiPass = 3
}

/// <summary>
/// Describes how the number of rectification passes is chosen.
/// </summary>
public enum IsoXmlGuidanceRectificationPassSelectionMode
{
    Automatic = 0,
    Manual = 1
}
