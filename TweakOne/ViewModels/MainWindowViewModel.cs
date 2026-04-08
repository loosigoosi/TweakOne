using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using TweakOne.IsoXml.V3;
using TweakOne.Localization;

namespace TweakOne.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    internal const double BasePreviewCanvasSize = 320d;
    private const double MinimumPreviewZoom = 0.5d;
    private const double MaximumPreviewZoom = 8d;
    private readonly IsoXmlGuidancePathGenerator _generator = new();
    private readonly IsoXmlGuidanceRectificationService _rectificationService = new();
    private readonly IsoXmlCenteredRectificationService _centeredRectificationService = new();
    private readonly IsoXmlCenteredRectificationTemplateInjectionService _centeredRectificationTemplateInjectionService = new();
    private TaskDocumentViewModel? _sourceDocument;
    private TaskDocumentViewModel? _targetDocument;
    private IsoXmlTaskDataDocument? _centeredRectificationTemplateDocument;
    private PartfieldViewModel? _selectedSourcePartfield;
    private PartfieldViewModel? _selectedTargetPartfield;
    private GuidancePathViewModel? _selectedSourceGuidancePath;
    private GuidancePathViewModel? _selectedRectificationSourceGuidancePath;
    private GuidancePathViewModel? _selectedTargetGuidancePath;
    private IsoXmlGuidanceRectificationResult? _currentRectificationResult;
    private string? _sourceFilePath;
    private string? _targetFilePath;
    private string _cloneDesignator = string.Empty;
    private double _translationOffsetMeters = 10d;
    private double _cloneSourcePreviewZoom = 1d;
    private double _cloneTargetPreviewZoom = 1d;
    private double _cloneSourcePreviewOffsetX;
    private double _cloneSourcePreviewOffsetY;
    private double _cloneTargetPreviewOffsetX;
    private double _cloneTargetPreviewOffsetY;
    private string _rectificationDesignator = string.Empty;
    private double _rectificationOffsetMeters = 0.75d;
    private int _rectificationRowCount = 4;
    private double _rectificationToleranceMeters = 0.10d;
    private IsoXmlGuidanceRectificationPassSelectionMode _rectificationPassSelectionMode = IsoXmlGuidanceRectificationPassSelectionMode.Automatic;
    private RectificationGuidanceExportMode _rectificationGuidanceExportMode = RectificationGuidanceExportMode.Single;
    private int _rectificationManualPassCount = 2;
    private RectificationCandidateViewModel? _selectedRectificationCandidate;
    private CenteredRectificationPackageViewModel? _selectedCenteredRectificationPackage;
    private CenteredRectificationOffsetRowViewModel? _selectedCenteredRectificationOffsetRow;
    private string? _centeredRectificationTemplatePath;
    private double _centeredRectificationMarkerDistanceMeters = 9d;
    private double _centeredRectificationPreviewZoom = 1d;
    private double _centeredRectificationPreviewOffsetX;
    private double _centeredRectificationPreviewOffsetY;
    private double _rectificationSourcePreviewZoom = 1d;
    private double _rectificationTargetPreviewZoom = 1d;
    private double _rectificationSourcePreviewOffsetX;
    private double _rectificationSourcePreviewOffsetY;
    private double _rectificationTargetPreviewOffsetX;
    private double _rectificationTargetPreviewOffsetY;
    private string _rectificationResultDisplay = LocalizedStrings.Instance.RectificationResultNotAnalyzed;
    private string _centeredRectificationSummaryDisplay = LocalizedStrings.Instance.CenteredRectificationNoAcceptedPackage;
    private string _statusMessage = LocalizedStrings.Instance.StatusInitial;

    public LocalizedStrings Strings { get; } = LocalizedStrings.Instance;

    public ObservableCollection<PartfieldViewModel> SourcePartfields { get; } = new();

    public ObservableCollection<PartfieldViewModel> TargetPartfields { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> CloneSourcePreviewShapes { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> CloneTargetPreviewShapes { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> RectificationSourcePreviewShapes { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> RectificationTargetPreviewShapes { get; } = new();

    public ObservableCollection<RectificationCandidateViewModel> RectificationCandidates { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> CenteredRectificationPreviewShapes { get; } = new();

    public ObservableCollection<CenteredRectificationPackageViewModel> CenteredRectificationPackages { get; } = new();

    public ObservableCollection<CenteredRectificationOffsetRowViewModel> CenteredRectificationOffsetRows { get; } = new();

    public IReadOnlyList<int> RectificationManualPassCounts { get; } = Enumerable.Range(1, IsoXmlGuidanceRectificationService.MaximumManualPassCount).ToArray();

    public string? SourceFilePath
    {
        get => _sourceFilePath;
        private set
        {
            if (SetProperty(ref _sourceFilePath, value))
            {
                OnPropertyChanged(nameof(SourceFilePathDisplay));
            }
        }
    }

    public string? TargetFilePath
    {
        get => _targetFilePath;
        private set
        {
            if (SetProperty(ref _targetFilePath, value))
            {
                OnPropertyChanged(nameof(TargetFilePathDisplay));
            }
        }
    }

    public bool IsRectificationPassModeAutomatic
    {
        get => _rectificationPassSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Automatic;
        set
        {
            if (value)
            {
                SetRectificationPassSelectionMode(IsoXmlGuidanceRectificationPassSelectionMode.Automatic);
            }
        }
    }

    public bool IsRectificationPassModeManual
    {
        get => _rectificationPassSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Manual;
        set
        {
            if (value)
            {
                SetRectificationPassSelectionMode(IsoXmlGuidanceRectificationPassSelectionMode.Manual);
            }
        }
    }

    public bool IsRectificationManualPassCountEnabled => _rectificationPassSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Manual;

    public bool IsRectificationAutomaticPassCountVisible => _rectificationPassSelectionMode == IsoXmlGuidanceRectificationPassSelectionMode.Automatic;

    public bool IsRectificationExportModeSingle
    {
        get => _rectificationGuidanceExportMode == RectificationGuidanceExportMode.Single;
        set
        {
            if (value)
            {
                SetRectificationGuidanceExportMode(RectificationGuidanceExportMode.Single);
            }
        }
    }

    public bool IsRectificationExportModeGrouped
    {
        get => _rectificationGuidanceExportMode == RectificationGuidanceExportMode.Grouped;
        set
        {
            if (value)
            {
                SetRectificationGuidanceExportMode(RectificationGuidanceExportMode.Grouped);
            }
        }
    }

    public bool IsRectificationExportModeTramlines
    {
        get => _rectificationGuidanceExportMode == RectificationGuidanceExportMode.Tramlines;
        set
        {
            if (value)
            {
                SetRectificationGuidanceExportMode(RectificationGuidanceExportMode.Tramlines);
            }
        }
    }

    public bool IsRectificationTramlinesExportModeEnabled => false;

    public CenteredRectificationPackageViewModel? SelectedCenteredRectificationPackage
    {
        get => _selectedCenteredRectificationPackage;
        set
        {
            if (SetProperty(ref _selectedCenteredRectificationPackage, value))
            {
                ResetCenteredRectificationPreviewPan();
                RefreshCenteredRectificationPlan();
            }
        }
    }

    public CenteredRectificationOffsetRowViewModel? SelectedCenteredRectificationOffsetRow
    {
        get => _selectedCenteredRectificationOffsetRow;
        set
        {
            if (SetProperty(ref _selectedCenteredRectificationOffsetRow, value))
            {
                RefreshCenteredRectificationPreview();
            }
        }
    }

    public int RectificationManualPassCount
    {
        get => _rectificationManualPassCount;
        set
        {
            var clampedValue = Math.Clamp(value, RectificationManualPassCounts[0], RectificationManualPassCounts[^1]);
            if (SetProperty(ref _rectificationManualPassCount, clampedValue))
            {
                InvalidateRectificationResult();
            }
        }
    }

    public string SourceFilePathDisplay => SourceFilePath ?? Strings.NoSourceFileLoaded;

    public string TargetFilePathDisplay => TargetFilePath ?? Strings.NoTargetFileLoaded;

    public PartfieldViewModel? SelectedSourcePartfield
    {
        get => _selectedSourcePartfield;
        set
        {
            if (!SetProperty(ref _selectedSourcePartfield, value))
            {
                return;
            }

            SelectedSourceGuidancePath = value?.GuidancePaths.FirstOrDefault();
            SelectedRectificationSourceGuidancePath = value?.GuidancePaths.FirstOrDefault();
            ResetCloneSourcePreviewPan();
            ResetRectificationSourcePreviewPan();
            RefreshCloneSourcePreview();
            RefreshRectificationSourcePreview();
            OnPropertyChanged(nameof(SelectedSourcePartfieldSummaryDisplay));
            OnPropertyChanged(nameof(CanCloneGuidancePath));
            OnPropertyChanged(nameof(CanAnalyzeRectification));
            InvalidateRectificationResult();
        }
    }

    public PartfieldViewModel? SelectedTargetPartfield
    {
        get => _selectedTargetPartfield;
        set
        {
            if (!SetProperty(ref _selectedTargetPartfield, value))
            {
                return;
            }

            SelectedTargetGuidancePath = value?.GuidancePaths.LastOrDefault();
            ResetCloneTargetPreviewPan();
            ResetRectificationTargetPreviewPan();
            RefreshCloneTargetPreview();
            OnPropertyChanged(nameof(SelectedTargetPartfieldSummaryDisplay));
            OnPropertyChanged(nameof(CanCloneGuidancePath));
            OnPropertyChanged(nameof(CanDeleteTargetGuidancePath));
            OnPropertyChanged(nameof(CanAnalyzeRectification));
            OnPropertyChanged(nameof(CanApplyRectification));
            RefreshRectificationTargetPreview();
            InvalidateRectificationResult();
        }
    }

    public string SelectedSourcePartfieldSummaryDisplay => SelectedSourcePartfield?.Summary ?? Strings.SelectSourceField;

    public string SelectedTargetPartfieldSummaryDisplay => SelectedTargetPartfield?.Summary ?? Strings.SelectTargetField;

    public GuidancePathViewModel? SelectedSourceGuidancePath
    {
        get => _selectedSourceGuidancePath;
        set
        {
            if (!SetProperty(ref _selectedSourceGuidancePath, value))
            {
                return;
            }

            if (value is not null && string.IsNullOrWhiteSpace(CloneDesignator))
            {
                CloneDesignator = $"{value.DisplayName} copy";
            }

            RefreshCloneSourcePreview();
            OnPropertyChanged(nameof(CanCloneGuidancePath));
        }
    }

    public GuidancePathViewModel? SelectedRectificationSourceGuidancePath
    {
        get => _selectedRectificationSourceGuidancePath;
        set
        {
            if (!SetProperty(ref _selectedRectificationSourceGuidancePath, value))
            {
                return;
            }

            RefreshRectificationSourcePreview();
            OnPropertyChanged(nameof(CanAnalyzeRectification));
            InvalidateRectificationResult();
        }
    }

    public RectificationCandidateViewModel? SelectedRectificationCandidate
    {
        get => _selectedRectificationCandidate;
        set
        {
            if (SetProperty(ref _selectedRectificationCandidate, value))
            {
                OnPropertyChanged(nameof(CanApplyRectification));
                RefreshRectificationTargetPreview();
            }
        }
    }

    public GuidancePathViewModel? SelectedTargetGuidancePath
    {
        get => _selectedTargetGuidancePath;
        set
        {
            if (!SetProperty(ref _selectedTargetGuidancePath, value))
            {
                return;
            }

            RefreshCloneTargetPreview();
            OnPropertyChanged(nameof(CanDeleteTargetGuidancePath));
        }
    }

    public string CloneDesignator
    {
        get => _cloneDesignator;
        set => SetProperty(ref _cloneDesignator, value);
    }

    public double TranslationOffsetMeters
    {
        get => _translationOffsetMeters;
        set => SetProperty(ref _translationOffsetMeters, value);
    }

    public double CloneSourcePreviewZoom
    {
        get => _cloneSourcePreviewZoom;
        set
        {
            if (SetPreviewZoom(ref _cloneSourcePreviewZoom, value, nameof(CloneSourcePreviewZoom), nameof(CloneSourcePreviewZoomDisplay), nameof(CloneSourcePreviewCanvasSize)))
            {
                RefreshCloneSourcePreview();
            }
        }
    }

    public string CloneSourcePreviewZoomDisplay => FormatPreviewZoom(CloneSourcePreviewZoom);

    public double CloneSourcePreviewCanvasSize => BasePreviewCanvasSize * CloneSourcePreviewZoom;

    public double CloneSourcePreviewOffsetX
    {
        get => _cloneSourcePreviewOffsetX;
        private set => SetProperty(ref _cloneSourcePreviewOffsetX, value);
    }

    public double CloneSourcePreviewOffsetY
    {
        get => _cloneSourcePreviewOffsetY;
        private set => SetProperty(ref _cloneSourcePreviewOffsetY, value);
    }

    public double CloneTargetPreviewZoom
    {
        get => _cloneTargetPreviewZoom;
        set
        {
            if (SetPreviewZoom(ref _cloneTargetPreviewZoom, value, nameof(CloneTargetPreviewZoom), nameof(CloneTargetPreviewZoomDisplay), nameof(CloneTargetPreviewCanvasSize)))
            {
                RefreshCloneTargetPreview();
            }
        }
    }

    public string CloneTargetPreviewZoomDisplay => FormatPreviewZoom(CloneTargetPreviewZoom);

    public double CloneTargetPreviewCanvasSize => BasePreviewCanvasSize * CloneTargetPreviewZoom;

    public double CloneTargetPreviewOffsetX
    {
        get => _cloneTargetPreviewOffsetX;
        private set => SetProperty(ref _cloneTargetPreviewOffsetX, value);
    }

    public double CloneTargetPreviewOffsetY
    {
        get => _cloneTargetPreviewOffsetY;
        private set => SetProperty(ref _cloneTargetPreviewOffsetY, value);
    }

    public string RectificationDesignator
    {
        get => _rectificationDesignator;
        set
        {
            if (SetProperty(ref _rectificationDesignator, value))
            {
                OnPropertyChanged(nameof(CanAnalyzeRectification));
                OnPropertyChanged(nameof(CanApplyRectification));
                InvalidateRectificationResult();
            }
        }
    }

    public double RectificationSourcePreviewZoom
    {
        get => _rectificationSourcePreviewZoom;
        set
        {
            if (SetPreviewZoom(ref _rectificationSourcePreviewZoom, value, nameof(RectificationSourcePreviewZoom), nameof(RectificationSourcePreviewZoomDisplay), nameof(RectificationSourcePreviewCanvasSize)))
            {
                RefreshRectificationSourcePreview();
            }
        }
    }

    public string RectificationSourcePreviewZoomDisplay => FormatPreviewZoom(RectificationSourcePreviewZoom);

    public double RectificationSourcePreviewCanvasSize => BasePreviewCanvasSize * RectificationSourcePreviewZoom;

    public double RectificationSourcePreviewOffsetX
    {
        get => _rectificationSourcePreviewOffsetX;
        private set => SetProperty(ref _rectificationSourcePreviewOffsetX, value);
    }

    public double RectificationSourcePreviewOffsetY
    {
        get => _rectificationSourcePreviewOffsetY;
        private set => SetProperty(ref _rectificationSourcePreviewOffsetY, value);
    }

    public double RectificationTargetPreviewZoom
    {
        get => _rectificationTargetPreviewZoom;
        set
        {
            if (SetPreviewZoom(ref _rectificationTargetPreviewZoom, value, nameof(RectificationTargetPreviewZoom), nameof(RectificationTargetPreviewZoomDisplay), nameof(RectificationTargetPreviewCanvasSize)))
            {
                RefreshRectificationTargetPreview();
            }
        }
    }

    public string RectificationTargetPreviewZoomDisplay => FormatPreviewZoom(RectificationTargetPreviewZoom);

    public double RectificationTargetPreviewCanvasSize => BasePreviewCanvasSize * RectificationTargetPreviewZoom;

    public double RectificationTargetPreviewOffsetX
    {
        get => _rectificationTargetPreviewOffsetX;
        private set => SetProperty(ref _rectificationTargetPreviewOffsetX, value);
    }

    public double RectificationTargetPreviewOffsetY
    {
        get => _rectificationTargetPreviewOffsetY;
        private set => SetProperty(ref _rectificationTargetPreviewOffsetY, value);
    }

    public double RectificationOffsetMeters
    {
        get => _rectificationOffsetMeters;
        set
        {
            if (SetProperty(ref _rectificationOffsetMeters, value))
            {
                OnPropertyChanged(nameof(RectificationApplicationOffsetDisplay));
                InvalidateRectificationResult();
            }
        }
    }

    public int RectificationRowCount
    {
        get => _rectificationRowCount;
        set
        {
            if (SetProperty(ref _rectificationRowCount, value))
            {
                OnPropertyChanged(nameof(RectificationApplicationOffsetDisplay));
                InvalidateRectificationResult();
            }
        }
    }

    public double RectificationToleranceMeters
    {
        get => _rectificationToleranceMeters;
        set
        {
            if (SetProperty(ref _rectificationToleranceMeters, value))
            {
                InvalidateRectificationResult();
            }
        }
    }

    public string RectificationResultDisplay
    {
        get => _rectificationResultDisplay;
        private set => SetProperty(ref _rectificationResultDisplay, value);
    }

    public string RectificationApplicationOffsetDisplay => (RectificationOffsetMeters * RectificationRowCount).ToString("0.##");

    public double CenteredRectificationPreviewZoom
    {
        get => _centeredRectificationPreviewZoom;
        set
        {
            if (SetPreviewZoom(ref _centeredRectificationPreviewZoom, value, nameof(CenteredRectificationPreviewZoom), nameof(CenteredRectificationPreviewZoomDisplay), nameof(CenteredRectificationPreviewCanvasSize)))
            {
                RefreshCenteredRectificationPreview();
            }
        }
    }

    public string CenteredRectificationPreviewZoomDisplay => FormatPreviewZoom(CenteredRectificationPreviewZoom);

    public double CenteredRectificationPreviewCanvasSize => BasePreviewCanvasSize * CenteredRectificationPreviewZoom;

    public double CenteredRectificationPreviewOffsetX
    {
        get => _centeredRectificationPreviewOffsetX;
        private set => SetProperty(ref _centeredRectificationPreviewOffsetX, value);
    }

    public double CenteredRectificationPreviewOffsetY
    {
        get => _centeredRectificationPreviewOffsetY;
        private set => SetProperty(ref _centeredRectificationPreviewOffsetY, value);
    }

    public double CenteredRectificationMarkerDistanceMeters
    {
        get => _centeredRectificationMarkerDistanceMeters;
        set
        {
            if (SetProperty(ref _centeredRectificationMarkerDistanceMeters, Math.Max(0d, value)))
            {
                RefreshCenteredRectificationPlan();
            }
        }
    }

    public string CenteredRectificationTemplatePathDisplay => _centeredRectificationTemplatePath ?? Strings.CenteredRectificationNoTemplateLoaded;

    public string CenteredRectificationAcceptedPackageDisplay => SelectedCenteredRectificationPackage?.DisplayName ?? Strings.CenteredRectificationNoAcceptedPackage;

    public string CenteredRectificationSummaryDisplay
    {
        get => _centeredRectificationSummaryDisplay;
        private set => SetProperty(ref _centeredRectificationSummaryDisplay, value);
    }

    public string CenteredRectificationMachineWidthDisplay => SelectedCenteredRectificationPackage?.MachineWidthMeters.ToString("0.##") ?? Strings.CenteredRectificationNotAvailable;

    public bool CanUseCenteredRectification => SelectedCenteredRectificationPackage is not null;

    public bool CanApplyCenteredRectificationTemplate => _centeredRectificationTemplateDocument is not null && SelectedCenteredRectificationPackage?.Plan is not null;

    public string RectificationAutomaticPassCountDisplay
    {
        get
        {
            if (_currentRectificationResult is null)
            {
                return Strings.RectificationAutomaticPassCountNotAnalyzed;
            }

            var requiredPassCount = _currentRectificationResult.Candidates.Max(candidate => candidate.RequiredPassCount);
            return requiredPassCount <= IsoXmlGuidanceRectificationService.MaximumAutomaticPassCount
                ? Strings.FormatRectificationAutomaticPassCount(requiredPassCount)
                : Strings.FormatRectificationAutomaticPassCountUnsupported(requiredPassCount, IsoXmlGuidanceRectificationService.MaximumAutomaticPassCount);
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        internal set => SetProperty(ref _statusMessage, value);
    }

    public bool CanCloneGuidancePath => SelectedSourceGuidancePath is not null && SelectedTargetPartfield is not null;

    public bool CanUseSourceAsTarget => _sourceDocument is not null;

    public bool CanDeleteTargetGuidancePath => SelectedTargetGuidancePath is not null && SelectedTargetPartfield is not null;

    public bool CanAnalyzeRectification => SelectedRectificationSourceGuidancePath is not null && SelectedTargetPartfield is not null && HasRectificationDesignator;

    public bool CanApplyRectification => SelectedRectificationCandidate?.Candidate.IsAccepted == true && SelectedTargetPartfield is not null && HasRectificationDesignator;

    public void LoadSourceDocument(string filePath, string? displayPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var document = IsoXmlTaskDataSerializer.Load(filePath);
        _sourceDocument = TaskDocumentViewModel.Create(document);
        ReplacePartfields(SourcePartfields, _sourceDocument.Partfields);
        SourceFilePath = displayPath ?? filePath;
        SelectedSourcePartfield = SourcePartfields.FirstOrDefault(static partfield => partfield.GuidancePaths.Count > 0)
            ?? SourcePartfields.FirstOrDefault();
        OnPropertyChanged(nameof(CanUseSourceAsTarget));
        StatusMessage = Strings.FormatLoadedSourcePackage(SourceFilePathDisplay);
    }

    public void LoadTargetDocument(string filePath, string? displayPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var document = IsoXmlTaskDataSerializer.Load(filePath);
        _targetDocument = TaskDocumentViewModel.Create(document);
        ReplacePartfields(TargetPartfields, _targetDocument.Partfields);
        TargetFilePath = displayPath ?? filePath;
        SelectedTargetPartfield = TargetPartfields.FirstOrDefault();
        StatusMessage = Strings.FormatLoadedTargetPackage(TargetFilePathDisplay);
    }

    public void SaveTargetDocument(string filePath, string? displayPath = null)
    {
        ArgumentNullException.ThrowIfNull(_targetDocument);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        IsoXmlTaskDataSerializer.Save(_targetDocument.Document, filePath);
        TargetFilePath = displayPath ?? filePath;
        StatusMessage = Strings.FormatSavedTargetPackage(TargetFilePathDisplay);
    }

    public void CreateSimpleClone()
    {
        var source = SelectedSourceGuidancePath ?? throw new InvalidOperationException(Strings.SelectSourceGuidanceLineError);
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);

        var copy = _generator.CreateSimpleCopy(target.Partfield, source.LineString, GetCloneDesignator(source));
        target.Refresh();
        SelectedTargetGuidancePath = target.GuidancePaths.First(path => ReferenceEquals(path.LineString, copy));
        StatusMessage = Strings.FormatClonedGuidanceLine(source.DisplayName, target.DisplayName);
    }

    public void CreateTranslatedClone()
    {
        var source = SelectedSourceGuidancePath ?? throw new InvalidOperationException(Strings.SelectSourceGuidanceLineError);
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);

        var copy = _generator.CreateTranslatedCopy(target.Partfield, source.LineString, TranslationOffsetMeters, GetCloneDesignator(source));
        target.Refresh();
        SelectedTargetGuidancePath = target.GuidancePaths.First(path => ReferenceEquals(path.LineString, copy));
        StatusMessage = Strings.FormatClonedAndTranslatedGuidanceLine(source.DisplayName, TranslationOffsetMeters, target.DisplayName);
    }

    public void DeleteSelectedTargetGuidancePath()
    {
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);
        var selected = SelectedTargetGuidancePath ?? throw new InvalidOperationException(Strings.SelectTargetGuidanceLineError);

        if (!target.Partfield.LineStrings.Remove(selected.LineString))
        {
            throw new InvalidOperationException(Strings.SelectTargetGuidanceLineError);
        }

        target.Refresh();
        SelectedTargetGuidancePath = target.GuidancePaths.LastOrDefault();
        StatusMessage = Strings.FormatDeletedTargetGuidanceLine(selected.DisplayName, target.DisplayName);
    }

    public void AnalyzeRectification()
    {
        var source = SelectedRectificationSourceGuidancePath ?? throw new InvalidOperationException(Strings.SelectSourceGuidanceLineError);
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);

        var result = _rectificationService.AnalyzeRectification(
            target.Partfield,
            source.LineString,
            RectificationOffsetMeters,
            RectificationRowCount,
            RectificationToleranceMeters,
            RequireRectificationDesignator(),
            _rectificationPassSelectionMode,
            RectificationManualPassCount);

        _currentRectificationResult = result;
        RectificationCandidates.Clear();
        foreach (var candidate in result.Candidates)
        {
            RectificationCandidates.Add(new RectificationCandidateViewModel(candidate, CreateRectificationCandidateSummary(candidate)));
        }

        SelectedRectificationCandidate = RectificationCandidates.FirstOrDefault();
        RectificationResultDisplay = string.Join(Environment.NewLine, result.Candidates.Select(CreateRectificationCandidateSummary));
        OnPropertyChanged(nameof(RectificationAutomaticPassCountDisplay));
        RefreshRectificationTargetPreview();
        OnPropertyChanged(nameof(CanApplyRectification));
        StatusMessage = RectificationResultDisplay;
    }

    public void ApplyRectification()
    {
        var source = SelectedRectificationSourceGuidancePath ?? throw new InvalidOperationException(Strings.SelectSourceGuidanceLineError);
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);
        var selectedCandidate = SelectedRectificationCandidate?.Candidate ?? throw new InvalidOperationException(RectificationResultDisplay);
        if (!selectedCandidate.IsAccepted)
        {
            throw new InvalidOperationException(RectificationResultDisplay);
        }

        PersistCenteredRectificationPackages(source, new[] { selectedCandidate });

        var appliedLineCount = 0;
        var appliedGroupCount = 0;
        GuidancePathViewModel? lastApplied = null;

        switch (_rectificationGuidanceExportMode)
        {
            case RectificationGuidanceExportMode.Single:
                foreach (var line in selectedCandidate.GeneratedLines)
                {
                    var applied = line.DeepClone();
                    target.Partfield.LineStrings.Add(applied);
                    lastApplied = new GuidancePathViewModel(applied);
                    appliedLineCount++;
                }

                break;

            case RectificationGuidanceExportMode.Grouped:
                _generator.CreateGroupedRectification(target.Partfield, selectedCandidate.GeneratedLines, selectedCandidate.CandidateLine.Designator);
                appliedLineCount += selectedCandidate.GeneratedLines.Count;
                appliedGroupCount++;

                break;

            case RectificationGuidanceExportMode.Tramlines:
                throw new InvalidOperationException(Strings.RectificationTramlinesExportNotAvailable);

            default:
                throw new InvalidOperationException($"Unsupported rectification export mode '{_rectificationGuidanceExportMode}'.");
        }

        target.Refresh();
        if (lastApplied is not null)
        {
            SelectedTargetGuidancePath = target.GuidancePaths.LastOrDefault(path => path.DisplayName == lastApplied.DisplayName) ?? target.GuidancePaths.LastOrDefault();
        }

        StatusMessage = _rectificationGuidanceExportMode == RectificationGuidanceExportMode.Grouped
            ? Strings.FormatAppliedRectifiedGuidanceGroups(appliedGroupCount, appliedLineCount, target.DisplayName)
            : Strings.FormatAppliedRectifiedGuidanceLines(appliedLineCount, target.DisplayName);
        InvalidateRectificationResult();
    }

    public void LoadCenteredRectificationTemplate(string filePath, string? displayPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var document = IsoXmlTaskDataSerializer.Load(filePath);
        if (!string.Equals(document.VersionMajor, "4", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(Strings.CenteredRectificationTemplateVersionError);
        }

        _centeredRectificationTemplateDocument = document;
        _centeredRectificationTemplatePath = displayPath ?? filePath;
        OnPropertyChanged(nameof(CenteredRectificationTemplatePathDisplay));
        OnPropertyChanged(nameof(CanApplyCenteredRectificationTemplate));
        StatusMessage = Strings.FormatLoadedCenteredRectificationTemplate(CenteredRectificationTemplatePathDisplay);
    }

    public void ApplyCenteredRectificationTemplate(string filePath, string? displayPath = null)
    {
        var document = _centeredRectificationTemplateDocument ?? throw new InvalidOperationException(Strings.CenteredRectificationNoTemplateLoaded);
        var package = SelectedCenteredRectificationPackage ?? throw new InvalidOperationException(Strings.CenteredRectificationNoPlanError);
        var plan = package.Plan ?? throw new InvalidOperationException(Strings.CenteredRectificationNoPlanError);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var result = _centeredRectificationTemplateInjectionService.Inject(
            document,
            package.SourcePartfield.Identifier,
            package.SourcePartfield.DisplayName,
            plan,
            package.GeneratedLines);

        IsoXmlTaskDataSerializer.Save(document, filePath);
        var savedPath = displayPath ?? filePath;
        var partfieldName = string.IsNullOrWhiteSpace(result.TargetPartfield.Designator)
            ? result.TargetPartfield.Id ?? Strings.UnnamedField
            : result.TargetPartfield.Designator;
        StatusMessage = Strings.FormatAppliedCenteredRectificationTemplate(result.InjectedGuidanceLineCount, result.InjectedMarkerLineCount, partfieldName, savedPath);
    }

    private static void ReplacePartfields(ObservableCollection<PartfieldViewModel> target, IEnumerable<PartfieldViewModel> partfields)
    {
        target.Clear();
        foreach (var partfield in partfields)
        {
            target.Add(partfield);
        }
    }

    private void PersistCenteredRectificationPackages(GuidancePathViewModel source, IReadOnlyList<IsoXmlGuidanceRectificationCandidateResult> acceptedCandidates)
    {
        CenteredRectificationPackages.Clear();
        SelectedCenteredRectificationPackage = null;

        var sourcePartfield = SelectedSourcePartfield;
        if (sourcePartfield is null)
        {
            return;
        }

        var machineWidthMeters = RectificationOffsetMeters * RectificationRowCount;
        foreach (var candidate in acceptedCandidates)
        {
            var package = new CenteredRectificationPackageViewModel(
                sourcePartfield,
                source.LineString.DeepClone(),
                candidate.GeneratedLines.Select(static line => line.DeepClone()).ToArray(),
                machineWidthMeters,
                candidate.SignedApplicationOffsetMeters >= 0d
                    ? Strings.PositiveDirectionLabel
                    : Strings.NegativeDirectionLabel,
                string.IsNullOrWhiteSpace(RectificationDesignator) ? source.DisplayName : RectificationDesignator.Trim());

            CenteredRectificationPackages.Add(package);
        }

        SelectedCenteredRectificationPackage = CenteredRectificationPackages.FirstOrDefault();
        OnPropertyChanged(nameof(CanUseCenteredRectification));
        OnPropertyChanged(nameof(CanApplyCenteredRectificationTemplate));
    }

    private void RefreshCenteredRectificationPlan()
    {
        CenteredRectificationOffsetRows.Clear();
        CenteredRectificationPreviewShapes.Clear();
        OnPropertyChanged(nameof(CenteredRectificationMachineWidthDisplay));
        OnPropertyChanged(nameof(CanUseCenteredRectification));
        OnPropertyChanged(nameof(CanApplyCenteredRectificationTemplate));

        var package = SelectedCenteredRectificationPackage;
        if (package is null)
        {
            CenteredRectificationSummaryDisplay = Strings.CenteredRectificationNoAcceptedPackage;
            return;
        }

        var plan = _centeredRectificationService.BuildPlan(
            package.SourcePartfield.Partfield,
            package.ReferenceLine,
            package.GeneratedLines,
            package.MachineWidthMeters,
            CenteredRectificationMarkerDistanceMeters,
            package.BaseDesignator);

        package.Plan = plan;
        OnPropertyChanged(nameof(CanApplyCenteredRectificationTemplate));

        foreach (var row in plan.OffsetRows)
        {
            CenteredRectificationOffsetRows.Add(new CenteredRectificationOffsetRowViewModel(row));
        }

        SelectedCenteredRectificationOffsetRow = CenteredRectificationOffsetRows.FirstOrDefault();
        RefreshCenteredRectificationPreview();

        CenteredRectificationSummaryDisplay = Strings.FormatCenteredRectificationSummary(plan.OffsetRows.Count, plan.MachineWidthMeters, plan.MarkerDistanceMeters, package.DirectionLabel);
    }

    private void SetRectificationGuidanceExportMode(RectificationGuidanceExportMode value)
    {
        if (SetProperty(ref _rectificationGuidanceExportMode, value, nameof(IsRectificationExportModeSingle)))
        {
            OnPropertyChanged(nameof(IsRectificationExportModeSingle));
            OnPropertyChanged(nameof(IsRectificationExportModeGrouped));
            OnPropertyChanged(nameof(IsRectificationExportModeTramlines));
            OnPropertyChanged(nameof(IsRectificationTramlinesExportModeEnabled));
        }
    }

    private string GetCloneDesignator(GuidancePathViewModel source)
    {
        return string.IsNullOrWhiteSpace(CloneDesignator)
            ? Strings.FormatCloneDesignator(source.DisplayName)
            : CloneDesignator.Trim();
    }

    private string RequireRectificationDesignator()
    {
        if (string.IsNullOrWhiteSpace(RectificationDesignator))
        {
            throw new InvalidOperationException(Strings.RectificationDesignatorRequiredError);
        }

        return RectificationDesignator.Trim();
    }

    private void SetRectificationPassSelectionMode(IsoXmlGuidanceRectificationPassSelectionMode value)
    {
        if (SetProperty(ref _rectificationPassSelectionMode, value, nameof(IsRectificationPassModeAutomatic)))
        {
            OnPropertyChanged(nameof(IsRectificationPassModeAutomatic));
            OnPropertyChanged(nameof(IsRectificationPassModeManual));
            OnPropertyChanged(nameof(IsRectificationManualPassCountEnabled));
            OnPropertyChanged(nameof(IsRectificationAutomaticPassCountVisible));
            InvalidateRectificationResult();
        }
    }

    private string CreateRectificationCandidateSummary(IsoXmlGuidanceRectificationCandidateResult candidate)
    {
        var desiredOffset = Math.Abs(candidate.SignedApplicationOffsetMeters);
        var detail = candidate.Mode switch
        {
            IsoXmlGuidanceRectificationMode.Standard => Strings.FormatRectificationAcceptedSummary(candidate.MinDistanceMeters, candidate.MaxDistanceMeters, desiredOffset, RectificationToleranceMeters, candidate.MaxDeviationMeters),
            IsoXmlGuidanceRectificationMode.TwoPass => Strings.FormatRectificationTwoPassSummary(candidate.MinDistanceMeters, candidate.MaxDistanceMeters, desiredOffset, RectificationToleranceMeters, candidate.MaxDeviationMeters, candidate.ExcessDeviationMeters, candidate.ExcessDeviationMeters / 2d),
            IsoXmlGuidanceRectificationMode.MultiPass => Strings.FormatRectificationMultiPassSummary(candidate.GeneratedLines.Count, candidate.MinDistanceMeters, candidate.MaxDistanceMeters, desiredOffset, RectificationToleranceMeters, candidate.MaxDeviationMeters, candidate.ExcessDeviationMeters, candidate.ExcessDeviationMeters / candidate.GeneratedLines.Count),
            _ => Strings.FormatRectificationRejectedSummary(candidate.MinDistanceMeters, candidate.MaxDistanceMeters, desiredOffset, RectificationToleranceMeters, candidate.MaxDeviationMeters)
        };

        var directionLabel = candidate.SignedApplicationOffsetMeters >= 0d
            ? Strings.PositiveDirectionLabel
            : Strings.NegativeDirectionLabel;

        return Strings.FormatRectificationCandidateSummary(directionLabel, detail);
    }

    public void ResetCloneSourcePreviewZoom()
    {
        CloneSourcePreviewZoom = 1d;
        ResetCloneSourcePreviewPan();
    }

    public void ResetCloneTargetPreviewZoom()
    {
        CloneTargetPreviewZoom = 1d;
        ResetCloneTargetPreviewPan();
    }

    public void ResetRectificationSourcePreviewZoom()
    {
        RectificationSourcePreviewZoom = 1d;
        ResetRectificationSourcePreviewPan();
    }

    public void ResetRectificationTargetPreviewZoom()
    {
        RectificationTargetPreviewZoom = 1d;
        ResetRectificationTargetPreviewPan();
    }

    public void ResetCenteredRectificationPreviewZoom()
    {
        CenteredRectificationPreviewZoom = 1d;
        ResetCenteredRectificationPreviewPan();
    }

    public void SetCloneSourcePreviewPan(double x, double y)
    {
        CloneSourcePreviewOffsetX = x;
        CloneSourcePreviewOffsetY = y;
    }

    public void TranslateCloneSourcePreviewPan(double deltaX, double deltaY)
    {
        SetCloneSourcePreviewPan(CloneSourcePreviewOffsetX + deltaX, CloneSourcePreviewOffsetY + deltaY);
    }

    public void SetCloneTargetPreviewPan(double x, double y)
    {
        CloneTargetPreviewOffsetX = x;
        CloneTargetPreviewOffsetY = y;
    }

    public void TranslateCloneTargetPreviewPan(double deltaX, double deltaY)
    {
        SetCloneTargetPreviewPan(CloneTargetPreviewOffsetX + deltaX, CloneTargetPreviewOffsetY + deltaY);
    }

    public void SetRectificationSourcePreviewPan(double x, double y)
    {
        RectificationSourcePreviewOffsetX = x;
        RectificationSourcePreviewOffsetY = y;
    }

    public void TranslateRectificationSourcePreviewPan(double deltaX, double deltaY)
    {
        SetRectificationSourcePreviewPan(RectificationSourcePreviewOffsetX + deltaX, RectificationSourcePreviewOffsetY + deltaY);
    }

    public void SetRectificationTargetPreviewPan(double x, double y)
    {
        RectificationTargetPreviewOffsetX = x;
        RectificationTargetPreviewOffsetY = y;
    }

    public void TranslateRectificationTargetPreviewPan(double deltaX, double deltaY)
    {
        SetRectificationTargetPreviewPan(RectificationTargetPreviewOffsetX + deltaX, RectificationTargetPreviewOffsetY + deltaY);
    }

    public void SetCenteredRectificationPreviewPan(double x, double y)
    {
        CenteredRectificationPreviewOffsetX = x;
        CenteredRectificationPreviewOffsetY = y;
    }

    public void TranslateCenteredRectificationPreviewPan(double deltaX, double deltaY)
    {
        SetCenteredRectificationPreviewPan(CenteredRectificationPreviewOffsetX + deltaX, CenteredRectificationPreviewOffsetY + deltaY);
    }

    private void RefreshCloneSourcePreview()
    {
        ReplacePreviewShapes(CloneSourcePreviewShapes, SelectedSourcePartfield, SelectedSourceGuidancePath, CloneSourcePreviewZoom);
    }

    private void RefreshCloneTargetPreview()
    {
        ReplacePreviewShapes(CloneTargetPreviewShapes, SelectedTargetPartfield, SelectedTargetGuidancePath, CloneTargetPreviewZoom);
    }

    private void RefreshRectificationSourcePreview()
    {
        ReplacePreviewShapes(RectificationSourcePreviewShapes, SelectedSourcePartfield, SelectedRectificationSourceGuidancePath, RectificationSourcePreviewZoom);
    }

    private void RefreshRectificationTargetPreview()
    {
        ReplacePreviewShapes(
            RectificationTargetPreviewShapes,
            SelectedTargetPartfield,
            null,
            RectificationTargetPreviewZoom,
            CreateRectificationPreviewOverlays());
    }

    private void RefreshCenteredRectificationPreview()
    {
        var package = SelectedCenteredRectificationPackage;
        if (package is null)
        {
            CenteredRectificationPreviewShapes.Clear();
            return;
        }

        ReplacePreviewShapes(
            CenteredRectificationPreviewShapes,
            package.SourcePartfield,
            null,
            CenteredRectificationPreviewZoom,
            CreateCenteredRectificationPreviewOverlays(package));
    }

    private IReadOnlyList<PreviewOverlay>? CreateCenteredRectificationPreviewOverlays(CenteredRectificationPackageViewModel package)
    {
        if (package.Plan is null)
        {
            return null;
        }

        var selectedPassNumber = SelectedCenteredRectificationOffsetRow?.PassNumber;
        var overlays = new List<PreviewOverlay>
        {
            new(package.ReferenceLine, Brushes.Goldenrod),
            new(package.Plan.MarkerLineA, Brushes.Red),
            new(package.Plan.MarkerLineB, Brushes.Red)
        };

        for (var index = 0; index < package.GeneratedLines.Count; index++)
        {
            var passNumber = index + 1;
            overlays.Add(new PreviewOverlay(
                package.GeneratedLines[index],
                selectedPassNumber == passNumber ? Brushes.Orange : Brushes.MediumSeaGreen));
        }

        return overlays;
    }

    private IReadOnlyList<PreviewOverlay>? CreateRectificationPreviewOverlays()
    {
        if (_currentRectificationResult is null)
        {
            return null;
        }

        var selectedCandidate = SelectedRectificationCandidate?.Candidate;
        return _currentRectificationResult.Candidates
            .SelectMany(CreateCandidateOverlays)
            .ToArray();

        IEnumerable<PreviewOverlay> CreateCandidateOverlays(IsoXmlGuidanceRectificationCandidateResult candidate)
        {
            var isSelected = selectedCandidate is not null && ReferenceEquals(selectedCandidate, candidate);
            return CreateRectificationPreviewOverlays(candidate, isSelected);
        }
    }

    private IEnumerable<PreviewOverlay> CreateRectificationPreviewOverlays(IsoXmlGuidanceRectificationCandidateResult candidate, bool isSelected)
    {
        for (var index = 0; index < candidate.GeneratedLines.Count; index++)
        {
            var isFinalPass = index == (candidate.GeneratedLines.Count - 1);
            yield return new PreviewOverlay(
                candidate.GeneratedLines[index],
                GetRectificationOverlayBrush(isSelected, isFinalPass));
        }
    }

    private void InvalidateRectificationResult()
    {
        _currentRectificationResult = null;
        RectificationCandidates.Clear();
        SelectedRectificationCandidate = null;
        RectificationResultDisplay = Strings.RectificationResultNotAnalyzed;
        OnPropertyChanged(nameof(RectificationAutomaticPassCountDisplay));
        RefreshRectificationTargetPreview();
        OnPropertyChanged(nameof(CanApplyRectification));
    }

    private bool HasRectificationDesignator => !string.IsNullOrWhiteSpace(RectificationDesignator);

    private static IBrush GetRectificationOverlayBrush(bool isSelected, bool isFinalPass)
    {
        if (isSelected)
        {
            return isFinalPass ? Brushes.Orange : Brushes.DarkOrange;
        }

        return isFinalPass ? Brushes.LimeGreen : Brushes.MediumSeaGreen;
    }

    private bool SetPreviewZoom(ref double field, double value, string zoomPropertyName, string zoomDisplayPropertyName, string zoomCanvasSizePropertyName)
    {
        var clampedValue = Math.Clamp(value, MinimumPreviewZoom, MaximumPreviewZoom);
        if (SetProperty(ref field, clampedValue, zoomPropertyName))
        {
            OnPropertyChanged(zoomDisplayPropertyName);
            OnPropertyChanged(zoomCanvasSizePropertyName);
            return true;
        }

        return false;
    }

    private static string FormatPreviewZoom(double zoom)
    {
        return $"{zoom * 100d:0}%";
    }

    private void ResetCloneSourcePreviewPan()
    {
        SetCloneSourcePreviewPan(0d, 0d);
    }

    private void ResetCloneTargetPreviewPan()
    {
        SetCloneTargetPreviewPan(0d, 0d);
    }

    private void ResetRectificationSourcePreviewPan()
    {
        SetRectificationSourcePreviewPan(0d, 0d);
    }

    private void ResetRectificationTargetPreviewPan()
    {
        SetRectificationTargetPreviewPan(0d, 0d);
    }

    private void ResetCenteredRectificationPreviewPan()
    {
        SetCenteredRectificationPreviewPan(0d, 0d);
    }

    private static void ReplacePreviewShapes(ObservableCollection<PreviewPolylineViewModel> target, PartfieldViewModel? partfield, GuidancePathViewModel? highlightedGuidancePath, double zoom, IReadOnlyList<PreviewOverlay>? overlays = null)
    {
        target.Clear();
        if (partfield is null)
        {
            return;
        }

        foreach (var preview in partfield.CreatePreview(highlightedGuidancePath?.LineString, zoom, overlays))
        {
            target.Add(preview);
        }
    }

    internal readonly record struct PreviewOverlay(IsoXmlLineString LineString, IBrush Stroke);
}

internal sealed class TaskDocumentViewModel
{
    private TaskDocumentViewModel(IsoXmlTaskDataDocument document, IReadOnlyList<PartfieldViewModel> partfields)
    {
        Document = document;
        Partfields = partfields;
    }

    public IsoXmlTaskDataDocument Document { get; }

    public IReadOnlyList<PartfieldViewModel> Partfields { get; }

    public static TaskDocumentViewModel Create(IsoXmlTaskDataDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new TaskDocumentViewModel(document, document.Partfields.Select(static partfield => new PartfieldViewModel(partfield)).ToArray());
    }
}

internal enum RectificationGuidanceExportMode
{
    Single = 0,
    Tramlines = 1,
    Grouped = 2
}

internal sealed class PartfieldViewModel : ObservableObject
{
    private readonly ObservableCollection<GuidancePathViewModel> _guidancePaths = new();

    public PartfieldViewModel(IsoXmlPartfield partfield)
    {
        Partfield = partfield ?? throw new ArgumentNullException(nameof(partfield));
        GuidancePaths = new ReadOnlyObservableCollection<GuidancePathViewModel>(_guidancePaths);
        Refresh();
    }

    public IsoXmlPartfield Partfield { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Partfield.Designator) ? Partfield.Id ?? LocalizedStrings.Instance.UnnamedField : Partfield.Designator!;

    public string Identifier => Partfield.Id ?? string.Empty;

    public string Summary => LocalizedStrings.Instance.FormatPartfieldSummary(Partfield.Area, _guidancePaths.Count);

    public ReadOnlyObservableCollection<GuidancePathViewModel> GuidancePaths { get; }

    public void Refresh()
    {
        _guidancePaths.Clear();
        foreach (var line in Partfield.GuidancePaths)
        {
            _guidancePaths.Add(new GuidancePathViewModel(line));
        }

        OnPropertyChanged(nameof(Summary));
    }

    public IReadOnlyList<PreviewPolylineViewModel> CreatePreview(IsoXmlLineString? highlightedGuidancePath, double zoom, IReadOnlyList<MainWindowViewModel.PreviewOverlay>? overlays = null)
    {
        var rawShapes = CreateRawShapes(highlightedGuidancePath, overlays);
        return Normalize(rawShapes, zoom);
    }

    private List<RawPreviewShape> CreateRawShapes(IsoXmlLineString? highlightedGuidancePath, IReadOnlyList<MainWindowViewModel.PreviewOverlay>? overlays)
    {
        var rawShapes = new List<RawPreviewShape>();

        foreach (var boundary in Partfield.Polygons.SelectMany(static polygon => polygon.LineStrings))
        {
            if (boundary.Points.Count < 2)
            {
                continue;
            }

            rawShapes.Add(new RawPreviewShape(boundary.Points.Select(static point => new GeoPoint(point.North, point.East)).ToArray(), Brushes.DimGray, 2d));
        }

        foreach (var guidancePath in Partfield.GuidancePaths)
        {
            if (guidancePath.Points.Count < 2)
            {
                continue;
            }

            var isHighlighted = ReferenceEquals(guidancePath, highlightedGuidancePath);
            rawShapes.Add(new RawPreviewShape(
                guidancePath.Points.Select(static point => new GeoPoint(point.North, point.East)).ToArray(),
                isHighlighted ? Brushes.Orange : Brushes.DodgerBlue,
                1d));
        }

        foreach (var markerLine in Partfield.LineStrings.Where(static line => line.Type == IsoXmlLineString.MarkerLineType))
        {
            if (markerLine.Points.Count < 2)
            {
                continue;
            }

            rawShapes.Add(new RawPreviewShape(
                markerLine.Points.Select(static point => new GeoPoint(point.North, point.East)).ToArray(),
                Brushes.Red,
                1d));
        }

        if (overlays is not null)
        {
            foreach (var overlay in overlays)
            {
                if (overlay.LineString.Points.Count < 2)
                {
                    continue;
                }

                rawShapes.Add(new RawPreviewShape(
                    overlay.LineString.Points.Select(static point => new GeoPoint(point.North, point.East)).ToArray(),
                    overlay.Stroke,
                    1d));
            }
        }

        return rawShapes;
    }

    private static IReadOnlyList<PreviewPolylineViewModel> Normalize(IReadOnlyList<RawPreviewShape> rawShapes, double zoom)
    {
        if (rawShapes.Count == 0)
        {
            return Array.Empty<PreviewPolylineViewModel>();
        }

        var width = MainWindowViewModel.BasePreviewCanvasSize * zoom;
        var height = MainWindowViewModel.BasePreviewCanvasSize * zoom;
        var padding = 16d * zoom;

        var minNorth = rawShapes.SelectMany(static shape => shape.Points).Min(static point => point.North);
        var maxNorth = rawShapes.SelectMany(static shape => shape.Points).Max(static point => point.North);
        var minEast = rawShapes.SelectMany(static shape => shape.Points).Min(static point => point.East);
        var maxEast = rawShapes.SelectMany(static shape => shape.Points).Max(static point => point.East);
        var latitudeMid = (minNorth + maxNorth) / 2d;
        var latitudeCos = Math.Cos(latitudeMid * Math.PI / 180d);
        if (Math.Abs(latitudeCos) < double.Epsilon)
        {
            latitudeCos = 1d;
        }

        var projectedShapes = rawShapes
            .Select(shape => new
            {
                shape.Stroke,
                shape.Thickness,
                Points = shape.Points.Select(point => new Point(point.East * latitudeCos, point.North)).ToArray()
            })
            .ToArray();

        var minX = projectedShapes.SelectMany(static shape => shape.Points).Min(static point => point.X);
        var maxX = projectedShapes.SelectMany(static shape => shape.Points).Max(static point => point.X);
        var minY = projectedShapes.SelectMany(static shape => shape.Points).Min(static point => point.Y);
        var maxY = projectedShapes.SelectMany(static shape => shape.Points).Max(static point => point.Y);
        var xSpan = Math.Max(maxX - minX, 1e-9);
        var ySpan = Math.Max(maxY - minY, 1e-9);
        var scale = Math.Min((width - (padding * 2d)) / xSpan, (height - (padding * 2d)) / ySpan);

        return projectedShapes
            .Select(shape => new PreviewPolylineViewModel(
                shape.Points.Select(point => new Point(
                    padding + ((point.X - minX) * scale),
                    height - padding - ((point.Y - minY) * scale))).ToArray(),
                shape.Stroke,
                shape.Thickness))
            .ToArray();
    }

    private readonly record struct RawPreviewShape(IReadOnlyList<GeoPoint> Points, IBrush Stroke, double Thickness);

    private readonly record struct GeoPoint(double North, double East);
}

internal sealed class GuidancePathViewModel
{
    public GuidancePathViewModel(IsoXmlLineString lineString)
    {
        LineString = lineString ?? throw new ArgumentNullException(nameof(lineString));
    }

    public IsoXmlLineString LineString { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(LineString.Designator) ? LocalizedStrings.Instance.GuidancePathDefaultName : LineString.Designator!;

    public string Summary
    {
        get
        {
            var start = LineString.Points.FirstOrDefault();
            var end = LineString.Points.LastOrDefault();
            if (start is null || end is null)
            {
                return LocalizedStrings.Instance.GuidancePathNoPoints;
            }

            return LocalizedStrings.Instance.FormatGuidancePathSummary(LineString.Points.Count, start.North, start.East, end.North, end.East);
        }
    }
}

internal sealed class CenteredRectificationPackageViewModel
{
    public CenteredRectificationPackageViewModel(PartfieldViewModel sourcePartfield, IsoXmlLineString referenceLine, IReadOnlyList<IsoXmlLineString> generatedLines, double machineWidthMeters, string directionLabel, string baseDesignator)
    {
        SourcePartfield = sourcePartfield ?? throw new ArgumentNullException(nameof(sourcePartfield));
        ReferenceLine = referenceLine ?? throw new ArgumentNullException(nameof(referenceLine));
        GeneratedLines = generatedLines ?? throw new ArgumentNullException(nameof(generatedLines));
        DirectionLabel = directionLabel ?? throw new ArgumentNullException(nameof(directionLabel));
        BaseDesignator = string.IsNullOrWhiteSpace(baseDesignator) ? LocalizedStrings.Instance.GuidancePathDefaultName : baseDesignator.Trim();
        MachineWidthMeters = machineWidthMeters;
    }

    public PartfieldViewModel SourcePartfield { get; }

    public IsoXmlLineString ReferenceLine { get; }

    public IReadOnlyList<IsoXmlLineString> GeneratedLines { get; }

    public double MachineWidthMeters { get; }

    public string DirectionLabel { get; }

    public string BaseDesignator { get; }

    public IsoXmlCenteredRectificationPlan? Plan { get; set; }

    public string DisplayName => $"{BaseDesignator} - {DirectionLabel}";
}

internal sealed class CenteredRectificationOffsetRowViewModel
{
    public CenteredRectificationOffsetRowViewModel(IsoXmlCenteredRectificationOffsetRow row)
    {
        Row = row;
    }

    public IsoXmlCenteredRectificationOffsetRow Row { get; }

    public int PassNumber => Row.PassNumber;

    public string OriginalDesignator => Row.OriginalDesignator;

    public string SuggestedDesignator => Row.SuggestedDesignator;

    public string OffsetADisplay => FormatOffset(Row.OffsetACentimeters);

    public string OffsetBDisplay => FormatOffset(Row.OffsetBCentimeters);

    private static string FormatOffset(int centimeters)
    {
        var sign = centimeters >= 0 ? "+" : "-";
        return $"{sign}{Math.Abs(centimeters):00} cm";
    }
}

internal sealed class RectificationCandidateViewModel
{
    public RectificationCandidateViewModel(IsoXmlGuidanceRectificationCandidateResult candidate, string summary)
    {
        Candidate = candidate ?? throw new ArgumentNullException(nameof(candidate));
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
    }

    public IsoXmlGuidanceRectificationCandidateResult Candidate { get; }

    public string Summary { get; }

    public string DirectionLabel => Candidate.SignedApplicationOffsetMeters >= 0d
        ? LocalizedStrings.Instance.PositiveDirectionLabel
        : LocalizedStrings.Instance.NegativeDirectionLabel;
}

internal sealed class PreviewPolylineViewModel
{
    public PreviewPolylineViewModel(IReadOnlyList<Point> points, IBrush stroke, double thickness)
    {
        Points = points ?? throw new ArgumentNullException(nameof(points));
        Stroke = stroke ?? throw new ArgumentNullException(nameof(stroke));
        Thickness = thickness;
    }

    public IReadOnlyList<Point> Points { get; }

    public IBrush Stroke { get; }

    public double Thickness { get; }
}

internal abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
