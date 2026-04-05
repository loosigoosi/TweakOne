using System.Globalization;
using System.Resources;

namespace TweakOne.Properties;

internal static class Resources
{
    private static readonly ResourceManager ResourceManagerInstance = new("TweakOne.Properties.Resources", typeof(Resources).Assembly);

    public static CultureInfo? Culture { get; set; }

    public static string WindowTitle => GetString(nameof(WindowTitle));
    public static string CloneTabHeader => GetString(nameof(CloneTabHeader));
    public static string RectificationTabHeader => GetString(nameof(RectificationTabHeader));
    public static string OpenSourcePackageButton => GetString(nameof(OpenSourcePackageButton));
    public static string NoSourceFileLoaded => GetString(nameof(NoSourceFileLoaded));
    public static string OpenTargetPackageButton => GetString(nameof(OpenTargetPackageButton));
    public static string SaveTargetPackageButton => GetString(nameof(SaveTargetPackageButton));
    public static string NoTargetFileLoaded => GetString(nameof(NoTargetFileLoaded));
    public static string SourceFieldHeader => GetString(nameof(SourceFieldHeader));
    public static string TargetFieldHeader => GetString(nameof(TargetFieldHeader));
    public static string SelectSourceField => GetString(nameof(SelectSourceField));
    public static string SelectTargetField => GetString(nameof(SelectTargetField));
    public static string GuidanceLinesHeader => GetString(nameof(GuidanceLinesHeader));
    public static string TargetGuidanceLinesHeader => GetString(nameof(TargetGuidanceLinesHeader));
    public static string PreviewZoomLabel => GetString(nameof(PreviewZoomLabel));
    public static string PreviewResetZoomButton => GetString(nameof(PreviewResetZoomButton));
    public static string PreviewPanHint => GetString(nameof(PreviewPanHint));
    public static string SourceGuidanceHint => GetString(nameof(SourceGuidanceHint));
    public static string TargetGuidanceHint => GetString(nameof(TargetGuidanceHint));
    public static string CloneControlsHeader => GetString(nameof(CloneControlsHeader));
    public static string CloneDesignatorLabel => GetString(nameof(CloneDesignatorLabel));
    public static string CloneDesignatorWatermark => GetString(nameof(CloneDesignatorWatermark));
    public static string TranslationOffsetMetersLabel => GetString(nameof(TranslationOffsetMetersLabel));
    public static string CloneAsIsButton => GetString(nameof(CloneAsIsButton));
    public static string CloneWithAutoTranslationButton => GetString(nameof(CloneWithAutoTranslationButton));
    public static string DeleteSelectedTargetGuidanceButton => GetString(nameof(DeleteSelectedTargetGuidanceButton));
    public static string AutoTranslationDescription => GetString(nameof(AutoTranslationDescription));
    public static string RectificationControlsHeader => GetString(nameof(RectificationControlsHeader));
    public static string RectificationDesignatorLabel => GetString(nameof(RectificationDesignatorLabel));
    public static string RectificationDesignatorWatermark => GetString(nameof(RectificationDesignatorWatermark));
    public static string RectificationOffsetMetersLabel => GetString(nameof(RectificationOffsetMetersLabel));
    public static string RectificationRowCountLabel => GetString(nameof(RectificationRowCountLabel));
    public static string RectificationApplicationOffsetMetersLabel => GetString(nameof(RectificationApplicationOffsetMetersLabel));
    public static string RectificationToleranceMetersLabel => GetString(nameof(RectificationToleranceMetersLabel));
    public static string AnalyzeRectificationButton => GetString(nameof(AnalyzeRectificationButton));
    public static string ApplyRectifiedGuidanceButton => GetString(nameof(ApplyRectifiedGuidanceButton));
    public static string RectificationResultHeader => GetString(nameof(RectificationResultHeader));
    public static string RectificationResultNotAnalyzed => GetString(nameof(RectificationResultNotAnalyzed));
    public static string RectificationDescription => GetString(nameof(RectificationDescription));
    public static string RectificationPreviewHint => GetString(nameof(RectificationPreviewHint));
    public static string StatusInitial => GetString(nameof(StatusInitial));
    public static string SaveTargetBeforeLoadingError => GetString(nameof(SaveTargetBeforeLoadingError));
    public static string SaveTargetPackagePickerTitle => GetString(nameof(SaveTargetPackagePickerTitle));
    public static string OpenSourcePackagePickerTitle => GetString(nameof(OpenSourcePackagePickerTitle));
    public static string OpenTargetPackagePickerTitle => GetString(nameof(OpenTargetPackagePickerTitle));
    public static string IsoXmlPackageFileType => GetString(nameof(IsoXmlPackageFileType));
    public static string DefaultTargetPackageFileName => GetString(nameof(DefaultTargetPackageFileName));
    public static string StatusLoadedSourcePackage => GetString(nameof(StatusLoadedSourcePackage));
    public static string StatusLoadedTargetPackage => GetString(nameof(StatusLoadedTargetPackage));
    public static string StatusSavedTargetPackage => GetString(nameof(StatusSavedTargetPackage));
    public static string SelectSourceGuidanceLineError => GetString(nameof(SelectSourceGuidanceLineError));
    public static string SelectTargetFieldError => GetString(nameof(SelectTargetFieldError));
    public static string SelectTargetGuidanceLineError => GetString(nameof(SelectTargetGuidanceLineError));
    public static string RectificationOffsetPositiveError => GetString(nameof(RectificationOffsetPositiveError));
    public static string RectificationTolerancePositiveError => GetString(nameof(RectificationTolerancePositiveError));
    public static string StatusClonedGuidanceLine => GetString(nameof(StatusClonedGuidanceLine));
    public static string StatusClonedAndTranslatedGuidanceLine => GetString(nameof(StatusClonedAndTranslatedGuidanceLine));
    public static string StatusDeletedTargetGuidanceLine => GetString(nameof(StatusDeletedTargetGuidanceLine));
    public static string StatusAppliedRectifiedGuidanceLines => GetString(nameof(StatusAppliedRectifiedGuidanceLines));
    public static string UnnamedField => GetString(nameof(UnnamedField));
    public static string PartfieldSummaryFormat => GetString(nameof(PartfieldSummaryFormat));
    public static string GuidancePathDefaultName => GetString(nameof(GuidancePathDefaultName));
    public static string GuidancePathNoPoints => GetString(nameof(GuidancePathNoPoints));
    public static string GuidancePathSummaryFormat => GetString(nameof(GuidancePathSummaryFormat));
    public static string GuidancePathCopyFormat => GetString(nameof(GuidancePathCopyFormat));
    public static string RectifiedGuidancePathFormat => GetString(nameof(RectifiedGuidancePathFormat));
    public static string RectifiedPositiveGuidancePathFormat => GetString(nameof(RectifiedPositiveGuidancePathFormat));
    public static string RectifiedNegativeGuidancePathFormat => GetString(nameof(RectifiedNegativeGuidancePathFormat));
    public static string PositiveDirectionLabel => GetString(nameof(PositiveDirectionLabel));
    public static string NegativeDirectionLabel => GetString(nameof(NegativeDirectionLabel));
    public static string RectificationCandidateSummaryFormat => GetString(nameof(RectificationCandidateSummaryFormat));
    public static string RectificationAcceptedSummaryFormat => GetString(nameof(RectificationAcceptedSummaryFormat));
    public static string RectificationRejectedSummaryFormat => GetString(nameof(RectificationRejectedSummaryFormat));
    public static string PackageNotFound => GetString(nameof(PackageNotFound));
    public static string PackageMissingTaskDataXml => GetString(nameof(PackageMissingTaskDataXml));
    public static string PackageInvalidTaskDataPath => GetString(nameof(PackageInvalidTaskDataPath));
    public static string GuidanceNeedsAtLeastTwoPointsError => GetString(nameof(GuidanceNeedsAtLeastTwoPointsError));
    public static string OnlyGuidancePathTypeError => GetString(nameof(OnlyGuidancePathTypeError));
    public static string TranslationNotSupportedAtPolesError => GetString(nameof(TranslationNotSupportedAtPolesError));
    public static string TranslationRequiresDistinctPointsError => GetString(nameof(TranslationRequiresDistinctPointsError));

    private static string GetString(string name)
    {
        return ResourceManagerInstance.GetString(name, Culture) ?? name;
    }
}
