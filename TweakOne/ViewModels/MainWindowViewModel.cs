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
    private const double MinimumPreviewZoom = 0.5d;
    private const double MaximumPreviewZoom = 4d;
    private readonly IsoXmlGuidancePathGenerator _generator = new();
    private readonly IsoXmlGuidanceRectificationService _rectificationService = new();
    private TaskDocumentViewModel? _sourceDocument;
    private TaskDocumentViewModel? _targetDocument;
    private PartfieldViewModel? _selectedSourcePartfield;
    private PartfieldViewModel? _selectedTargetPartfield;
    private GuidancePathViewModel? _selectedSourceGuidancePath;
    private GuidancePathViewModel? _selectedTargetGuidancePath;
    private IsoXmlGuidanceRectificationResult? _currentRectificationResult;
    private string? _sourceFilePath;
    private string? _targetFilePath;
    private string _cloneDesignator = string.Empty;
    private double _translationOffsetMeters = 10d;
    private double _cloneSourcePreviewZoom = 1d;
    private double _cloneTargetPreviewZoom = 1d;
    private string _rectificationDesignator = string.Empty;
    private double _rectificationOffsetMeters = 0.75d;
    private int _rectificationRowCount = 4;
    private double _rectificationToleranceMeters = 0.10d;
    private double _rectificationSourcePreviewZoom = 1d;
    private double _rectificationTargetPreviewZoom = 1d;
    private string _rectificationResultDisplay = LocalizedStrings.Instance.RectificationResultNotAnalyzed;
    private string _statusMessage = LocalizedStrings.Instance.StatusInitial;

    public LocalizedStrings Strings { get; } = LocalizedStrings.Instance;

    public ObservableCollection<PartfieldViewModel> SourcePartfields { get; } = new();

    public ObservableCollection<PartfieldViewModel> TargetPartfields { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> SourcePreviewShapes { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> TargetPreviewShapes { get; } = new();

    public ObservableCollection<PreviewPolylineViewModel> RectificationTargetPreviewShapes { get; } = new();

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
            RefreshSourcePreview();
            OnPropertyChanged(nameof(SelectedSourcePartfieldSummaryDisplay));
            OnPropertyChanged(nameof(CanCloneGuidancePath));
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
            RefreshTargetPreview();
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

            if (value is not null && string.IsNullOrWhiteSpace(RectificationDesignator))
            {
                RectificationDesignator = Strings.FormatRectifiedGuidanceDesignator(value.DisplayName);
            }

            RefreshSourcePreview();
            OnPropertyChanged(nameof(CanCloneGuidancePath));
            OnPropertyChanged(nameof(CanAnalyzeRectification));
            InvalidateRectificationResult();
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

            RefreshTargetPreview();
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
        set => SetPreviewZoom(ref _cloneSourcePreviewZoom, value, nameof(CloneSourcePreviewZoom), nameof(CloneSourcePreviewZoomDisplay));
    }

    public string CloneSourcePreviewZoomDisplay => FormatPreviewZoom(CloneSourcePreviewZoom);

    public double CloneTargetPreviewZoom
    {
        get => _cloneTargetPreviewZoom;
        set => SetPreviewZoom(ref _cloneTargetPreviewZoom, value, nameof(CloneTargetPreviewZoom), nameof(CloneTargetPreviewZoomDisplay));
    }

    public string CloneTargetPreviewZoomDisplay => FormatPreviewZoom(CloneTargetPreviewZoom);

    public string RectificationDesignator
    {
        get => _rectificationDesignator;
        set
        {
            if (SetProperty(ref _rectificationDesignator, value))
            {
                InvalidateRectificationResult();
            }
        }
    }

    public double RectificationSourcePreviewZoom
    {
        get => _rectificationSourcePreviewZoom;
        set => SetPreviewZoom(ref _rectificationSourcePreviewZoom, value, nameof(RectificationSourcePreviewZoom), nameof(RectificationSourcePreviewZoomDisplay));
    }

    public string RectificationSourcePreviewZoomDisplay => FormatPreviewZoom(RectificationSourcePreviewZoom);

    public double RectificationTargetPreviewZoom
    {
        get => _rectificationTargetPreviewZoom;
        set => SetPreviewZoom(ref _rectificationTargetPreviewZoom, value, nameof(RectificationTargetPreviewZoom), nameof(RectificationTargetPreviewZoomDisplay));
    }

    public string RectificationTargetPreviewZoomDisplay => FormatPreviewZoom(RectificationTargetPreviewZoom);

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

    public string StatusMessage
    {
        get => _statusMessage;
        internal set => SetProperty(ref _statusMessage, value);
    }

    public bool CanCloneGuidancePath => SelectedSourceGuidancePath is not null && SelectedTargetPartfield is not null;

    public bool CanDeleteTargetGuidancePath => SelectedTargetGuidancePath is not null && SelectedTargetPartfield is not null;

    public bool CanAnalyzeRectification => SelectedSourceGuidancePath is not null && SelectedTargetPartfield is not null;

    public bool CanApplyRectification => _currentRectificationResult is not null && _currentRectificationResult.Candidates.Any(static candidate => candidate.IsAccepted) && SelectedTargetPartfield is not null;

    public void LoadSourceDocument(string filePath, string? displayPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var document = IsoXmlTaskDataSerializer.Load(filePath);
        _sourceDocument = TaskDocumentViewModel.Create(document);
        ReplacePartfields(SourcePartfields, _sourceDocument.Partfields);
        SourceFilePath = displayPath ?? filePath;
        SelectedSourcePartfield = SourcePartfields.FirstOrDefault(static partfield => partfield.GuidancePaths.Count > 0)
            ?? SourcePartfields.FirstOrDefault();
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
        var source = SelectedSourceGuidancePath ?? throw new InvalidOperationException(Strings.SelectSourceGuidanceLineError);
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);

        var result = _rectificationService.AnalyzeRectification(
            target.Partfield,
            source.LineString,
            RectificationOffsetMeters,
            RectificationRowCount,
            RectificationToleranceMeters,
            GetRectificationDesignator(source));

        _currentRectificationResult = result;
        RectificationResultDisplay = string.Join(Environment.NewLine, result.Candidates.Select(CreateRectificationCandidateSummary));
        RefreshRectificationTargetPreview();
        OnPropertyChanged(nameof(CanApplyRectification));
        StatusMessage = RectificationResultDisplay;
    }

    public void ApplyRectification()
    {
        var target = SelectedTargetPartfield ?? throw new InvalidOperationException(Strings.SelectTargetFieldError);
        var result = _currentRectificationResult ?? throw new InvalidOperationException(Strings.RectificationResultNotAnalyzed);
        var acceptedCandidates = result.Candidates.Where(static candidate => candidate.IsAccepted).ToArray();
        if (acceptedCandidates.Length == 0)
        {
            throw new InvalidOperationException(RectificationResultDisplay);
        }

        GuidancePathViewModel? lastApplied = null;
        foreach (var candidate in acceptedCandidates)
        {
            var applied = candidate.CandidateLine.DeepClone();
            target.Partfield.LineStrings.Add(applied);
            lastApplied = new GuidancePathViewModel(applied);
        }

        target.Refresh();
        if (lastApplied is not null)
        {
            SelectedTargetGuidancePath = target.GuidancePaths.LastOrDefault(path => path.DisplayName == lastApplied.DisplayName) ?? target.GuidancePaths.LastOrDefault();
        }

        StatusMessage = Strings.FormatAppliedRectifiedGuidanceLines(acceptedCandidates.Length, target.DisplayName);
        InvalidateRectificationResult();
    }

    private static void ReplacePartfields(ObservableCollection<PartfieldViewModel> target, IEnumerable<PartfieldViewModel> partfields)
    {
        target.Clear();
        foreach (var partfield in partfields)
        {
            target.Add(partfield);
        }
    }

    private string GetCloneDesignator(GuidancePathViewModel source)
    {
        return string.IsNullOrWhiteSpace(CloneDesignator)
            ? Strings.FormatCloneDesignator(source.DisplayName)
            : CloneDesignator.Trim();
    }

    private string GetRectificationDesignator(GuidancePathViewModel source)
    {
        return string.IsNullOrWhiteSpace(RectificationDesignator)
            ? Strings.FormatRectifiedGuidanceDesignator(source.DisplayName)
            : RectificationDesignator.Trim();
    }

    private string CreateRectificationCandidateSummary(IsoXmlGuidanceRectificationCandidateResult candidate)
    {
        var detail = candidate.IsAccepted
            ? Strings.FormatRectificationAcceptedSummary(candidate.MinDistanceMeters, candidate.MaxDistanceMeters, RectificationOffsetMeters, RectificationToleranceMeters, candidate.MaxDeviationMeters)
            : Strings.FormatRectificationRejectedSummary(candidate.MinDistanceMeters, candidate.MaxDistanceMeters, RectificationOffsetMeters, RectificationToleranceMeters, candidate.MaxDeviationMeters);

        var directionLabel = candidate.SignedApplicationOffsetMeters >= 0d
            ? Strings.PositiveDirectionLabel
            : Strings.NegativeDirectionLabel;

        return Strings.FormatRectificationCandidateSummary(directionLabel, detail);
    }

    public void ResetCloneSourcePreviewZoom() => CloneSourcePreviewZoom = 1d;

    public void ResetCloneTargetPreviewZoom() => CloneTargetPreviewZoom = 1d;

    public void ResetRectificationSourcePreviewZoom() => RectificationSourcePreviewZoom = 1d;

    public void ResetRectificationTargetPreviewZoom() => RectificationTargetPreviewZoom = 1d;

    private void RefreshSourcePreview()
    {
        ReplacePreviewShapes(SourcePreviewShapes, SelectedSourcePartfield, SelectedSourceGuidancePath);
    }

    private void RefreshTargetPreview()
    {
        ReplacePreviewShapes(TargetPreviewShapes, SelectedTargetPartfield, SelectedTargetGuidancePath);
    }

    private void RefreshRectificationTargetPreview()
    {
        ReplacePreviewShapes(
            RectificationTargetPreviewShapes,
            SelectedTargetPartfield,
            null,
            _currentRectificationResult?.Candidates.Select(candidate => new PreviewOverlay(candidate.CandidateLine, candidate.SignedApplicationOffsetMeters >= 0d ? Brushes.LimeGreen : Brushes.YellowGreen)).ToArray());
    }

    private void InvalidateRectificationResult()
    {
        _currentRectificationResult = null;
        RectificationResultDisplay = Strings.RectificationResultNotAnalyzed;
        RefreshRectificationTargetPreview();
        OnPropertyChanged(nameof(CanApplyRectification));
    }

    private void SetPreviewZoom(ref double field, double value, string zoomPropertyName, string zoomDisplayPropertyName)
    {
        var clampedValue = Math.Clamp(value, MinimumPreviewZoom, MaximumPreviewZoom);
        if (SetProperty(ref field, clampedValue, zoomPropertyName))
        {
            OnPropertyChanged(zoomDisplayPropertyName);
        }
    }

    private static string FormatPreviewZoom(double zoom)
    {
        return $"{zoom * 100d:0}%";
    }

    private static void ReplacePreviewShapes(ObservableCollection<PreviewPolylineViewModel> target, PartfieldViewModel? partfield, GuidancePathViewModel? highlightedGuidancePath, IReadOnlyList<PreviewOverlay>? overlays = null)
    {
        target.Clear();
        if (partfield is null)
        {
            return;
        }

        foreach (var preview in partfield.CreatePreview(highlightedGuidancePath?.LineString, overlays))
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

    public IReadOnlyList<PreviewPolylineViewModel> CreatePreview(IsoXmlLineString? highlightedGuidancePath, IReadOnlyList<MainWindowViewModel.PreviewOverlay>? overlays = null)
    {
        var rawShapes = CreateRawShapes(highlightedGuidancePath, overlays);
        return Normalize(rawShapes);
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

    private static IReadOnlyList<PreviewPolylineViewModel> Normalize(IReadOnlyList<RawPreviewShape> rawShapes)
    {
        if (rawShapes.Count == 0)
        {
            return Array.Empty<PreviewPolylineViewModel>();
        }

        const double width = 320d;
        const double height = 320d;
        const double padding = 16d;

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
