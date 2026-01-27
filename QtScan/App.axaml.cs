using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using QtScan.Infrastructure;
#if IOS
using QtScan.Infrastructure.Ios;
#else
using QtScan.Infrastructure.OpenCv;
#endif
using QtScan.UI;
using QtScan.UI.ViewModels;

namespace QtScan;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var scanner =
#if IOS
            new IosQrScanner();
#else
            new OpenCvQrScanner();
#endif
        ;
        var decoder =
#if IOS
            new IosQrDecoder();
#else
            new OpenCvQrDecoder();
#endif
        ;
        var generator = new QrCodeGeneratorService();
        var viewModel = new MainViewModel(scanner, decoder, generator);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow(viewModel);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = new MainView { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
