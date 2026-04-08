using System.Globalization;
using TweakOne.Properties;

namespace TweakOne.Localization;

internal sealed class LocalizedStrings
{
    public static LocalizedStrings Instance { get; } = new();

    public string WindowTitle => Resources.WindowTitle;

    public string CloneTabHeader => Resources.CloneTabHeader;

    public string RectificationTabHeader => Resources.RectificationTabHeader;

    public string CenteredRectificationTabHeader => Resources.CenteredRectificationTabHeader;

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

    public string UseSourceAsTargetButton => Resources.UseSourceAsTargetButton;

    public string AutoTranslationDescription => Resources.AutoTranslationDescription;

    public string RectificationControlsHeader => Resources.RectificationControlsHeader;

    public string RectificationDesignatorLabel => Resources.RectificationDesignatorLabel;

    public string RectificationDesignatorWatermark => Resources.RectificationDesignatorWatermark;

    public string RectificationDesignatorRequiredError => Resources.RectificationDesignatorRequiredError;

    public string RectificationOffsetMetersLabel => Resources.RectificationOffsetMetersLabel;

    public string RectificationRowCountLabel => Resources.RectificationRowCountLabel;

    public string RectificationApplicationOffsetMetersLabel => Resources.RectificationApplicationOffsetMetersLabel;

    public string RectificationToleranceMetersLabel => Resources.RectificationToleranceMetersLabel;

    public string RectificationPassModeLabel => Resources.RectificationPassModeLabel;

    public string RectificationPassModeAutomaticLabel => Resources.RectificationPassModeAutomaticLabel;

    public string RectificationPassModeManualLabel => Resources.RectificationPassModeManualLabel;

    public string RectificationManualPassCountLabel => Resources.RectificationManualPassCountLabel;

    public string RectificationAutomaticPassCountLabel => Resources.RectificationAutomaticPassCountLabel;

    public string RectificationAutomaticPassCountNotAnalyzed => Resources.RectificationAutomaticPassCountNotAnalyzed;

    public string RectificationToleranceGuidance => Resources.RectificationToleranceGuidance;

    public string RectificationExportModeLabel => Resources.RectificationExportModeLabel;

    public string RectificationExportModeSingleLabel => Resources.RectificationExportModeSingleLabel;

    public string RectificationExportModeGroupedLabel => Resources.RectificationExportModeGroupedLabel;

    public string RectificationExportModeTramlinesLabel => Resources.RectificationExportModeTramlinesLabel;

    public string RectificationGroupedExportHint => Resources.RectificationGroupedExportHint;

    public string AnalyzeRectificationButton => Resources.AnalyzeRectificationButton;

    public string ApplyRectifiedGuidanceButton => Resources.ApplyRectifiedGuidanceButton;

    public string RectificationResultHeader => Resources.RectificationResultHeader;

    public string RectificationResultNotAnalyzed => Resources.RectificationResultNotAnalyzed;

    public string RectificationDescription => Resources.RectificationDescription;

    public string RectificationPreviewHint => Resources.RectificationPreviewHint;

    public string CenteredRectificationPreviewHeader => Resources.CenteredRectificationPreviewHeader;

    public string CenteredRectificationPreviewHint => Resources.CenteredRectificationPreviewHint;

    public string CenteredRectificationControlsHeader => Resources.CenteredRectificationControlsHeader;

    public string OpenCenteredRectificationTemplateButton => Resources.OpenCenteredRectificationTemplateButton;

    public string ApplyCenteredRectificationTemplateButton => Resources.ApplyCenteredRectificationTemplateButton;

    public string OpenCenteredRectificationTemplatePickerTitle => Resources.OpenCenteredRectificationTemplatePickerTitle;

    public string SaveCenteredRectificationTemplatePickerTitle => Resources.SaveCenteredRectificationTemplatePickerTitle;

    public string CenteredRectificationTemplateLabel => Resources.CenteredRectificationTemplateLabel;

    public string CenteredRectificationAcceptedPackageLabel => Resources.CenteredRectificationAcceptedPackageLabel;

    public string CenteredRectificationMarkerDistanceLabel => Resources.CenteredRectificationMarkerDistanceLabel;

    public string CenteredRectificationMachineWidthLabel => Resources.CenteredRectificationMachineWidthLabel;

    public string CenteredRectificationOffsetsHeader => Resources.CenteredRectificationOffsetsHeader;

    public string CenteredRectificationNoTemplateLoaded => Resources.CenteredRectificationNoTemplateLoaded;

    public string CenteredRectificationTemplateMissingPartfieldError => Resources.CenteredRectificationTemplateMissingPartfieldError;

    public string CenteredRectificationNoPlanError => Resources.CenteredRectificationNoPlanError;

    public string CenteredRectificationCorrectionLineCountError => Resources.CenteredRectificationCorrectionLineCountError;

    public string CenteredRectificationNoAcceptedPackage => Resources.CenteredRectificationNoAcceptedPackage;

    public string CenteredRectificationNotAvailable => Resources.CenteredRectificationNotAvailable;

    public string CenteredRectificationMarkerALabel => Resources.CenteredRectificationMarkerALabel;

    public string CenteredRectificationMarkerBLabel => Resources.CenteredRectificationMarkerBLabel;

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

    public string FormatLoadedCenteredRectificationTemplate(string path) => Format(Resources.StatusLoadedCenteredRectificationTemplate, path);

    public string FormatAppliedCenteredRectificationTemplate(int guidanceLineCount, int markerLineCount, string partfieldName, string path) => Format(Resources.StatusAppliedCenteredRectificationTemplate, guidanceLineCount, markerLineCount, partfieldName, path);

    public string FormatClonedGuidanceLine(string guidanceLine, string targetField) => Format(Resources.StatusClonedGuidanceLine, guidanceLine, targetField);

    public string FormatClonedAndTranslatedGuidanceLine(string guidanceLine, double offsetMeters, string targetField) => Format(Resources.StatusClonedAndTranslatedGuidanceLine, guidanceLine, offsetMeters, targetField);

    public string FormatDeletedTargetGuidanceLine(string guidanceLine, string targetField) => Format(Resources.StatusDeletedTargetGuidanceLine, guidanceLine, targetField);

    public string FormatAppliedRectifiedGuidanceLines(int count, string targetField) => Format(Resources.StatusAppliedRectifiedGuidanceLines, count, targetField);

    public string FormatAppliedRectifiedGuidanceGroups(int groupCount, int lineCount, string targetField) => Format(Resources.StatusAppliedRectifiedGuidanceGroups, groupCount, lineCount, targetField);

    public string FormatPartfieldSummary(ulong area, int guidanceLineCount) => Format(Resources.PartfieldSummaryFormat, area, guidanceLineCount);

    public string FormatGuidancePathSummary(int pointCount, double startNorth, double startEast, double endNorth, double endEast) => Format(Resources.GuidancePathSummaryFormat, pointCount, startNorth, startEast, endNorth, endEast);

    public string FormatCloneDesignator(string displayName) => Format(Resources.GuidancePathCopyFormat, displayName);

    public string FormatRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedGuidancePathFormat, displayName);

    public string FormatPositiveRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedPositiveGuidancePathFormat, displayName);

    public string FormatNegativeRectifiedGuidanceDesignator(string displayName) => Format(Resources.RectifiedNegativeGuidancePathFormat, displayName);

    public string FormatPositiveSmoothedGuidanceDesignator(string displayName) => Format(Resources.RectifiedPositiveSmoothedGuidancePathFormat, displayName);

    public string FormatNegativeSmoothedGuidanceDesignator(string displayName) => Format(Resources.RectifiedNegativeSmoothedGuidancePathFormat, displayName);

    public string FormatPositiveProgressiveGuidanceDesignator(string displayName, int passIndex, int passCount) => Format(Resources.RectifiedPositiveProgressiveGuidancePathFormat, displayName, passIndex, passCount);

    public string FormatNegativeProgressiveGuidanceDesignator(string displayName, int passIndex, int passCount) => Format(Resources.RectifiedNegativeProgressiveGuidancePathFormat, displayName, passIndex, passCount);

    public string FormatRectificationCandidateSummary(string directionLabel, string candidateSummary) => Format(Resources.RectificationCandidateSummaryFormat, directionLabel, candidateSummary);

    public string FormatRectificationAcceptedSummary(double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation) => Format(Resources.RectificationAcceptedSummaryFormat, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation);

    public string FormatRectificationAutomaticPassCount(int passCount) => Format(Resources.RectificationAutomaticPassCountFormat, passCount);

    public string FormatRectificationAutomaticPassCountUnsupported(int passCount, int maximumPassCount) => Format(Resources.RectificationAutomaticPassCountUnsupportedFormat, passCount, maximumPassCount);

    public string FormatCenteredRectificationSummary(int passCount, double machineWidthMeters, double markerDistanceMeters, string directionLabel) => Format(Resources.CenteredRectificationSummaryFormat, passCount, machineWidthMeters, markerDistanceMeters, directionLabel);

    public string FormatRectificationTwoPassSummary(double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation, double excessDeviation, double smoothingShift) => Format(Resources.RectificationTwoPassSummaryFormat, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation, excessDeviation, smoothingShift);

    public string FormatRectificationMultiPassSummary(int passCount, double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation, double excessDeviation, double excessDeviationPerPass) => Format(Resources.RectificationMultiPassSummaryFormat, passCount, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation, excessDeviation, excessDeviationPerPass);

    public string FormatRectificationRejectedSummary(double minDistance, double maxDistance, double desiredOffset, double tolerance, double maxDeviation) => Format(Resources.RectificationRejectedSummaryFormat, minDistance, maxDistance, desiredOffset, tolerance, maxDeviation);

    public string FormatRectificationPassCountRangeError(int minimumPassCount, int maximumPassCount) => Format(Resources.RectificationPassCountRangeErrorFormat, minimumPassCount, maximumPassCount);

    public string FormatPackageNotFound(string packagePath) => Format(Resources.PackageNotFound, packagePath);

    public string FormatPackageMissingTaskDataXml(string packagePath) => Format(Resources.PackageMissingTaskDataXml, packagePath);

    public string FormatPackageInvalidTaskDataPath(string packagePath) => Format(Resources.PackageInvalidTaskDataPath, packagePath);

    public string FormatOnlyGuidancePathTypeError(int guidancePathType) => Format(Resources.OnlyGuidancePathTypeError, guidancePathType);

    public string RectificationTramlinesExportNotAvailable => Resources.RectificationTramlinesExportNotAvailable;

    public string CenteredRectificationTemplateVersionError => Resources.CenteredRectificationTemplateVersionError;

    private static string Format(string format, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, format, arguments);
    }
}
