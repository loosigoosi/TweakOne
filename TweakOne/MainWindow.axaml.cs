using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TweakOne.Localization;
using TweakOne.ViewModels;

namespace TweakOne;

public partial class MainWindow : Window
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;
    private const double WheelZoomStep = 0.25d;

    private IsoXmlTaskDataPackage? _sourcePackage;
    private IsoXmlTaskDataPackage? _targetPackage;
    private InputElement? _activePreviewViewport;
    private Point _lastPreviewPointerPosition;

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;
    }

    internal MainWindowViewModel ViewModel { get; }

    private async void OpenSourceDocument_Click(object? sender, RoutedEventArgs e)
    {
        await LoadPackageAsync(isSource: true).ConfigureAwait(true);
    }

    private async void OpenTargetDocument_Click(object? sender, RoutedEventArgs e)
    {
        await LoadPackageAsync(isSource: false).ConfigureAwait(true);
    }

    private async void SaveTargetDocument_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var suggestedFileName = string.IsNullOrWhiteSpace(ViewModel.TargetFilePath)
                ? Strings.DefaultTargetPackageFileName
                : Path.GetFileName(ViewModel.TargetFilePath);

            if (_targetPackage is null)
            {
                throw new InvalidOperationException(Strings.SaveTargetBeforeLoadingError);
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Strings.SaveTargetPackagePickerTitle,
                SuggestedFileName = suggestedFileName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType(Strings.IsoXmlPackageFileType)
                    {
                        Patterns = new[] { "*.zip" },
                        MimeTypes = new[] { "application/zip" }
                    }
                }
            }).ConfigureAwait(true);

            var localPath = file?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            ViewModel.SaveTargetDocument(_targetPackage.TaskDataXmlPath, localPath);
            IsoXmlTaskDataPackageService.SaveAs(_targetPackage, localPath);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void UseSourceAsTarget_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_sourcePackage is null)
            {
                throw new InvalidOperationException(Strings.NoSourceFileLoaded);
            }

            var selectedSourcePartfieldIdentifier = ViewModel.SelectedSourcePartfield?.Identifier;
            var selectedSourcePartfieldDisplayName = ViewModel.SelectedSourcePartfield?.DisplayName;
            _targetPackage = ClonePackage(_sourcePackage);
            ViewModel.LoadTargetDocument(_targetPackage.TaskDataXmlPath, _sourcePackage.PackagePath);
            ViewModel.SelectedTargetPartfield = ViewModel.TargetPartfields.FirstOrDefault(partfield => partfield.Identifier == selectedSourcePartfieldIdentifier)
                ?? ViewModel.TargetPartfields.FirstOrDefault(partfield => partfield.DisplayName == selectedSourcePartfieldDisplayName)
                ?? ViewModel.SelectedTargetPartfield;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void CloneSimple_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.CreateSimpleClone();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void CloneTranslated_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.CreateTranslatedClone();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void DeleteSelectedTargetGuidance_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.DeleteSelectedTargetGuidancePath();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void ResetCloneSourcePreviewZoom_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ResetCloneSourcePreviewZoom();
    }

    private void ResetCloneTargetPreviewZoom_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ResetCloneTargetPreviewZoom();
    }

    private void ResetRectificationSourcePreviewZoom_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ResetRectificationSourcePreviewZoom();
    }

    private void ResetRectificationTargetPreviewZoom_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel.ResetRectificationTargetPreviewZoom();
    }

    private void CloneSourcePreview_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginPreviewPan(sender, e);
    }

    private void CloneSourcePreview_PointerMoved(object? sender, PointerEventArgs e)
    {
        UpdatePreviewPan(sender, ViewModel.TranslateCloneSourcePreviewPan, e);
    }

    private void CloneSourcePreview_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndPreviewPan(sender, e);
    }

    private void CloneTargetPreview_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginPreviewPan(sender, e);
    }

    private void CloneTargetPreview_PointerMoved(object? sender, PointerEventArgs e)
    {
        UpdatePreviewPan(sender, ViewModel.TranslateCloneTargetPreviewPan, e);
    }

    private void CloneTargetPreview_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndPreviewPan(sender, e);
    }

    private void RectificationSourcePreview_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginPreviewPan(sender, e);
    }

    private void RectificationSourcePreview_PointerMoved(object? sender, PointerEventArgs e)
    {
        UpdatePreviewPan(sender, ViewModel.TranslateRectificationSourcePreviewPan, e);
    }

    private void RectificationSourcePreview_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndPreviewPan(sender, e);
    }

    private void RectificationTargetPreview_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginPreviewPan(sender, e);
    }

    private void RectificationTargetPreview_PointerMoved(object? sender, PointerEventArgs e)
    {
        UpdatePreviewPan(sender, ViewModel.TranslateRectificationTargetPreviewPan, e);
    }

    private void RectificationTargetPreview_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        EndPreviewPan(sender, e);
    }

    private void PreviewViewport_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        _activePreviewViewport = null;
    }

    private void CloneSourcePreview_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ZoomPreviewAtPointer(sender, ViewModel.CloneSourcePreviewZoom, ViewModel.CloneSourcePreviewOffsetX, ViewModel.CloneSourcePreviewOffsetY, zoom => ViewModel.CloneSourcePreviewZoom = zoom, ViewModel.SetCloneSourcePreviewPan, e);
    }

    private void CloneTargetPreview_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ZoomPreviewAtPointer(sender, ViewModel.CloneTargetPreviewZoom, ViewModel.CloneTargetPreviewOffsetX, ViewModel.CloneTargetPreviewOffsetY, zoom => ViewModel.CloneTargetPreviewZoom = zoom, ViewModel.SetCloneTargetPreviewPan, e);
    }

    private void RectificationSourcePreview_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ZoomPreviewAtPointer(sender, ViewModel.RectificationSourcePreviewZoom, ViewModel.RectificationSourcePreviewOffsetX, ViewModel.RectificationSourcePreviewOffsetY, zoom => ViewModel.RectificationSourcePreviewZoom = zoom, ViewModel.SetRectificationSourcePreviewPan, e);
    }

    private void RectificationTargetPreview_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        ZoomPreviewAtPointer(sender, ViewModel.RectificationTargetPreviewZoom, ViewModel.RectificationTargetPreviewOffsetX, ViewModel.RectificationTargetPreviewOffsetY, zoom => ViewModel.RectificationTargetPreviewZoom = zoom, ViewModel.SetRectificationTargetPreviewPan, e);
    }

    private void AnalyzeRectification_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.AnalyzeRectification();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or ArgumentOutOfRangeException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void ApplyRectification_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ViewModel.ApplyRectification();
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private async Task LoadPackageAsync(bool isSource)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = isSource ? Strings.OpenSourcePackagePickerTitle : Strings.OpenTargetPackagePickerTitle,
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType(Strings.IsoXmlPackageFileType)
                    {
                        Patterns = new[] { "*.zip" },
                        MimeTypes = new[] { "application/zip" }
                    }
                }
            }).ConfigureAwait(true);

            var localPath = files.Count > 0 ? files[0].TryGetLocalPath() : null;
            if (string.IsNullOrWhiteSpace(localPath))
            {
                return;
            }

            var package = IsoXmlTaskDataPackageService.Open(localPath);
            if (isSource)
            {
                _sourcePackage = package;
                ViewModel.LoadSourceDocument(package.TaskDataXmlPath, package.PackagePath);
            }
            else
            {
                _targetPackage = package;
                ViewModel.LoadTargetDocument(package.TaskDataXmlPath, package.PackagePath);
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ArgumentException)
        {
            ViewModel.StatusMessage = exception.Message;
        }
    }

    private void BeginPreviewPan(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not InputElement viewport)
        {
            return;
        }

        if (!e.GetCurrentPoint(viewport).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _activePreviewViewport = viewport;
        _lastPreviewPointerPosition = e.GetPosition(viewport);
        e.Pointer.Capture(viewport);
        e.Handled = true;
    }

    private void UpdatePreviewPan(object? sender, Action<double, double> applyPanDelta, PointerEventArgs e)
    {
        if (sender is not InputElement viewport)
        {
            return;
        }

        if (!ReferenceEquals(_activePreviewViewport, viewport))
        {
            return;
        }

        var currentPosition = e.GetPosition(viewport);
        applyPanDelta(currentPosition.X - _lastPreviewPointerPosition.X, currentPosition.Y - _lastPreviewPointerPosition.Y);
        _lastPreviewPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void EndPreviewPan(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not InputElement viewport)
        {
            return;
        }

        if (!ReferenceEquals(_activePreviewViewport, viewport))
        {
            return;
        }

        e.Pointer.Capture(null);
        _activePreviewViewport = null;
        e.Handled = true;
    }

    private static void ZoomPreviewAtPointer(object? sender, double currentZoom, double currentOffsetX, double currentOffsetY, Action<double> setZoom, Action<double, double> setPan, PointerWheelEventArgs e)
    {
        if (sender is not InputElement viewport)
        {
            return;
        }

        var updatedZoom = AdjustZoom(currentZoom, e);
        if (Math.Abs(updatedZoom - currentZoom) <= double.Epsilon)
        {
            return;
        }

        var pointerPosition = e.GetPosition(viewport);
        var zoomRatio = updatedZoom / currentZoom;
        var updatedOffsetX = pointerPosition.X - ((pointerPosition.X - currentOffsetX) * zoomRatio);
        var updatedOffsetY = pointerPosition.Y - ((pointerPosition.Y - currentOffsetY) * zoomRatio);

        setZoom(updatedZoom);
        setPan(updatedOffsetX, updatedOffsetY);
    }

    private static double AdjustZoom(double currentZoom, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return currentZoom;
        }

        var delta = e.Delta.Y;
        if (Math.Abs(delta) <= double.Epsilon)
        {
            return currentZoom;
        }

        e.Handled = true;
        return currentZoom + (delta > 0d ? WheelZoomStep : -WheelZoomStep);
    }

    private static IsoXmlTaskDataPackage ClonePackage(IsoXmlTaskDataPackage sourcePackage)
    {
        ArgumentNullException.ThrowIfNull(sourcePackage);

        var clonedExtractionRootPath = Path.Combine(Path.GetTempPath(), "TweakOne", "Packages", Guid.NewGuid().ToString("N"));
        CopyDirectory(sourcePackage.ExtractionRootPath, clonedExtractionRootPath);

        var relativeTaskDataDirectoryPath = Path.GetRelativePath(sourcePackage.ExtractionRootPath, sourcePackage.TaskDataDirectoryPath);
        var clonedTaskDataDirectoryPath = Path.Combine(clonedExtractionRootPath, relativeTaskDataDirectoryPath);
        var clonedTaskDataXmlPath = Path.Combine(clonedTaskDataDirectoryPath, Path.GetFileName(sourcePackage.TaskDataXmlPath));

        return new IsoXmlTaskDataPackage(sourcePackage.PackagePath, clonedExtractionRootPath, clonedTaskDataDirectoryPath, clonedTaskDataXmlPath);
    }

    private static void CopyDirectory(string sourceDirectoryPath, string destinationDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectoryPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectoryPath);

        Directory.CreateDirectory(destinationDirectoryPath);

        foreach (var filePath in Directory.EnumerateFiles(sourceDirectoryPath))
        {
            var destinationFilePath = Path.Combine(destinationDirectoryPath, Path.GetFileName(filePath));
            File.Copy(filePath, destinationFilePath, overwrite: true);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(sourceDirectoryPath))
        {
            var destinationChildDirectoryPath = Path.Combine(destinationDirectoryPath, Path.GetFileName(directoryPath));
            CopyDirectory(directoryPath, destinationChildDirectoryPath);
        }
    }
}
