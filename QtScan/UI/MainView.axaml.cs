using Avalonia.Controls;
using Avalonia.Interactivity;
using QtScan.UI.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
#if IOS
using Foundation;
using PhotosUI;
using UniformTypeIdentifiers;
#endif

namespace QtScan.UI;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        var openButton = this.FindControl<Button>("btnOpenFile");
        if (openButton != null)
        {
            openButton.Click += OnOpenFile;
        }
    }

    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

#if IOS
        await ShowPhotoPickerAsync(viewModel);
#else
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Open QR Image",
            AllowMultiple = false,
            Filters =
            {
                new FileDialogFilter
                {
                    Name = "Images",
                    Extensions = { "png", "jpg", "jpeg", "bmp", "gif" }
                }
            }
        };

        var result = await dialog.ShowAsync(owner);
        if (result == null || result.Length == 0)
        {
            return;
        }

        var path = result[0];
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var bytes = await File.ReadAllBytesAsync(path);
        await viewModel.DecodeImageBytesAsync(bytes);
#endif
    }

#if IOS
    private static Task ShowPhotoPickerAsync(MainViewModel viewModel)
    {
        var tcs = new TaskCompletionSource<byte[]?>();
        var config = new PHPickerConfiguration
        {
            SelectionLimit = 1,
            Filter = PHPickerFilter.ImagesFilter
        };

        var picker = new PHPickerViewController(config);
        picker.Delegate = new PickerDelegate(tcs);

        var presenter = IosUiAccess.GetTopViewController();
        if (presenter == null)
        {
            tcs.TrySetResult(null);
            return tcs.Task;
        }

        presenter.PresentViewController(picker, true, null);

        return tcs.Task.ContinueWith(async task =>
        {
            var bytes = task.Result;
            if (bytes != null && bytes.Length > 0)
            {
                await viewModel.DecodeImageBytesAsync(bytes).ConfigureAwait(false);
            }
        }).Unwrap();
    }

    private sealed class PickerDelegate : PHPickerViewControllerDelegate
    {
        private readonly TaskCompletionSource<byte[]?> _tcs;

        public PickerDelegate(TaskCompletionSource<byte[]?> tcs)
        {
            _tcs = tcs;
        }

        public override void DidFinishPicking(PHPickerViewController picker, PHPickerResult[] results)
        {
            picker.DismissViewController(true, null);

            if (results == null || results.Length == 0)
            {
                _tcs.TrySetResult(null);
                return;
            }

            var provider = results[0].ItemProvider;
            if (!provider.HasItemConformingTo(UTType.Image.Identifier))
            {
                _tcs.TrySetResult(null);
                return;
            }

            provider.LoadDataRepresentation(UTType.Image.Identifier, (data, error) =>
            {
                if (error != null || data == null)
                {
                    _tcs.TrySetResult(null);
                    return;
                }

                _tcs.TrySetResult(data.ToArray());
            });
        }
    }

#endif
}
