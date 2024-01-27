using Maui.AppInstaller.ViewModel;

namespace Maui.AppInstaller;

public partial class MainPage : ContentPage
{
    #region Constructor

    public MainPage(MainPageViewModel mainPageViewModel)
    {
        InitializeComponent();
        this.BindingContext = mainPageViewModel;
    }

    //msbuild /restore /t:build /p:TargetFramework=net8.0-windows10.0.19041.0 /p:configuration=release /p:WindowsAppSDKSelfContained=true /p:WindowsPackageType=None /p:RuntimeIdentifier=win10-x64

    #endregion
}
