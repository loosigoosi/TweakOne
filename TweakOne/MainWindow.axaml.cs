using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TweakOne.Localization;
using TweakOne.ViewModels;

namespace TweakOne;

public partial class MainWindow : Window
{
    private static readonly LocalizedStrings Strings = LocalizedStrings.Instance;

    private IsoXmlTaskDataPackage? _sourcePackage;
    private IsoXmlTaskDataPackage? _targetPackage;

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
}
