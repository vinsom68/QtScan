#if IOS
using System.Linq;
using UIKit;

namespace QtScan.UI;

public static class IosUiAccess
{
    public static UIViewController? GetTopViewController()
    {
        var scene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .FirstOrDefault(s => s.ActivationState == UISceneActivationState.ForegroundActive);

        var window = scene?.Windows.FirstOrDefault(w => w.IsKeyWindow) ?? scene?.Windows.FirstOrDefault();
        var controller = window?.RootViewController;
        while (controller?.PresentedViewController != null)
        {
            controller = controller.PresentedViewController;
        }

        return controller;
    }
}
#endif
