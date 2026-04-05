using System.Globalization;
using TweakOne.Properties;

namespace TweakOne.Localization;

internal sealed class LocalizedStrings
{
    public static LocalizedStrings Instance { get; } = new();

    public string WindowTitle => Resources.WindowTitle;

    public string CloneTabHeader => Resources.CloneTabHeader;

    public string RectificationTabHeader => Resources.RectificationTabHeader;

    public string OpenSourcePackageButton => Resources.OpenSourcePackageButton;

    public string NoSourceFileLoaded => Resources.NoSourceFileLoaded;

    public string OpenTargetPackageButton => Resources.OpenTargetPackageButton;

    public string SaveTargetPackageButton => Resources.SaveTargetPackageButton;

    public string NoTargetFileLoaded => Resources.NoTargetFileLoaded;

    public string SourceFieldHeader => Resources.SourceFieldHeader;

    public string TargetFieldHeader => Resources.TargetFieldHeader;

    public string SelectSourceField => Resources.SelectSourceField;

    public string SelectTargetField => Resources.SelectTargetField;

    public string GuidanceLinesHeader => Resources.GuidanceLinesHeader;

    public string TargetGuidanceLinesHeader => Resources.TargetGuidanceLinesHeader;

    public string PreviewZoomLabel => Resources.PreviewZoomLabel;

    public string PreviewResetZoomButton => Resources.PreviewResetZoomButton;

    public string PreviewPanHint => Resources.PreviewPanHint;

    public string SourceGuidanceHint => Resources.SourceGuidanceHint;

    public string TargetGuidanceHint => Resources.TargetGuidanceHint;

    public string CloneControlsHeader => Resources.CloneControlsHeader;

    public string CloneDesignatorLabel => Resources.CloneDesignatorLabel;

    public string CloneDesignatorWatermark => Resources.CloneDesignatorWatermark;

    public string TranslationOffsetMetersLabel => Resources.TranslationOffsetMetersLabel;

    public string CloneAsIsButton => Resources.CloneAsIsButton;

    public string CloneWithAutoTranslationButton => Resources.CloneWithAutoTranslationButton;

    public string DeleteSelectedTargetGuidanceButton => Resources.DeleteSelectedTargetGuidanceButton;

    public string AutoTranslationDescription => Resources.AutoTranslationDescription;

    public string RectificationControlsHeader => Resources.RectificationControlsHeader;

    public string RectificationDesignatorLabel => Resources.RectificationDesignatorLabel;

    public string RectificationDesignatorWatermark => Resources.RectificationDesignatorWatermark;

    public string RectificationOffsetMetersLabel => Resources.RectificationOffsetMetersLabel;

    public string RectificationRowCountLabel => Resources.RectificationRowCountLabel;

    public string RectificationApplicationOffsetMetersLabel => Resources.RectificationApplicationOffsetMetersLabel;

    public string RectificationToleranceMetersLabel => Resources.RectificationToleranceMetersLabel;

    public string AnalyzeRectificationButton => Resources.AnalyzeRectificationButton;

    public string ApplyRectifiedGuidanceButton => Resources.ApplyRectifiedGuidanceButton;

    public string RectificationResultHeader => Resources.RectificationResultHeader;

    public string RectificationResultNotAnalyzed => Resources.RectificationResultNotAnalyzed;

    public string RectificationDescription => Resources.RectificationDescription;

    public string RectificationPreviewHint => Resources.RectificationPreviewHint;

    public string PositiveDirectionLabel => Resources.PositiveDirectionLabel;

    public string NegativeDirectionLabel => Resources.NegativeDirectionLabel;

    public string StatusInitial => Resources.StatusInitial;

    public string SaveTargetBeforeLoadingError => Resources.SaveTargetBeforeLoadingError;

    public string SaveTargetPackagePickerTitle => Resources.SaveTargetPackagePickerTitle;

    public string OpenSourcePackagePickerTitle => Resources.OpenSourcePackagePickerTitle;

    public string OpenTargetPackagePickerTitle => Resources.OpenTargetPackagePickerTitle;

    public string IsoXmlPackageFileType => Resources.IsoXmlPackageFileType;

    public string DefaultTargetPackageFileName => Resources.DefaultTargetPackageFileName;

    public string SelectSourceGuidanceLineError => Resources.SelectSourceGuidanceLineError;

    public string SelectTargetFieldError => Resources.SelectTargetFieldError;

    public string SelectTargetGuidanceLineError => Resources.SelectTargetGuidanceLineError;

    public string RectificationOffsetPositiveError => Resources.RectificationOffsetPositiveError;

    public string RectificationTolerancePositiveError => Resources.RectificationTolerancePositiveError;

    public string UnnamedField => Resources.UnnamedField;

    public string GuidancePathDefaultName => Resources.GuidancePathDefaultName;

    public string GuidancePathNoPoints => Resources.GuidancePathNoPoints;

    public string GuidanceNeedsAtLeastTwoPointsError => Resources.GuidanceNeedsAtLeastTwoPointsError;

    public string TranslationNotSupportedAtPolesError => Resources.TranslationNotSupportedAtPolesError;

    public string TranslationRequiresDistinctPointsError => Resources.TranslationRequiresDistinctPointsError;

    public string FormatLoadedSourcePackage(string path) => Format(Resources.StatusLoadedSourcePackage, path);

    public string FormatLoadedTargetPackage(string path) => Format(Resources.StatusLoadedTargetPackage, path);

    public string FormatSavedTargetPackage(string path) => Format(Resources.StatusSavedTargetPackage, path);

    public string FormatClonedGuidanceLine(string guidanceLine, string targetField) => Format(Resources.StatusClonedGuidanceLine, guidanceLine, targetField);

    public string FormatClonedAndTranslatedGuidanceLine(string guidanceLine, double offsetMeters, string targetField) => Format(Resources.StatusClonedAndTranslatedGuidanceLine, guidanceLine, offsetMeters, targetField);

    public string FormatDeletedTargetGuidanceLine(string guidanceLine, string targetField) => Format(Resources.StatusDeletedTargetGuidanceLine, guidanceLine, targetField);

    public string FormatAppliedRectifiedGuidanceLines(int count, string targetField) => Format(Resources.StatusAppliedRectifiedGuidanceLines, count, targetField);

    public string FormatPartfieldSummary(ulong area, int guidanceLineCount) => Format(Resources.PartfieldSummaryFormat, area, guidanceLineCount);

    public string FormatGuidancePathSummary(int pointCount, double startNorth, double startEast, double endNorth, double endEast) => Format(Resources.GuidancePathSummaryFormat, pointCount, startNorth, startEast, endNorth, endEast);

    public string FormatCloneDesignator(string displayName) => Format(Resources.GuidancePathCopyFormat, displayName);

    public string FormatRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedGuidancePathFormat, displayName);

    public string FormatPositiveRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedPositiveGuidancePathFormat, displayName);

    public string FormatNegativeRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedNegativeGuidancePathFormat, displayName);

    public string FormatRectificationCandidateSummary(string directionLabel, string candidateSummary) => Format(Resources.RectificationCandidateSummaryFormat, directionLabel, candidateSummary);

    public string FormatRectificationAcceptedSummary(double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation) => Format(Resources.RectificationAcceptedSummaryFormat, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation);

    public string FormatRectificationRejectedSummary(double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation) => Format(Resources.RectificationRejectedSummaryFormat, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation);

    public string FormatPackageNotFound(string packagePath) => Format(Resources.PackageNotFound, packagePath);

    public string FormatPackageMissingTaskDataXml(string packagePath) => Format(Resources.PackageMissingTaskDataXml, packagePath);

    public string FormatPackageInvalidTaskDataPath(string packagePath) => Format(Resources.PackageInvalidTaskDataPath, packagePath);

    public string FormatOnlyGuidancePathTypeError(int guidancePathType) => Format(Resources.OnlyGuidancePathTypeError, guidancePathType);

    private static string Format(string format, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, format, arguments);
    }
}
