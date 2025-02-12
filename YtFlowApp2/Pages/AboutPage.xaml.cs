using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace YtFlowApp2.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        // Called when the page is loaded
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CoreVersionText.Text = GetYtFlowCoreVersion().ToString();

            var pkgVer = Package.Current.Id.Version;
            var pkgVerStr = $"{pkgVer.Major}.{pkgVer.Minor}.{pkgVer.Build}.{pkgVer.Revision}";
            PackageVersionText.Text = pkgVerStr;
        }

        // Click event handler for License button
        private async void LicenseButton_Click(object sender, RoutedEventArgs e)
        {
            // Accessing the third-party markdown file (you can modify the Uri if the file location changes)
            var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///third_party.md"));

            var options = new LauncherOptions
            {
                DisplayApplicationPicker = true
            };

            // Launch the file using the default system app
            await Launcher.LaunchFileAsync(file, options);
        }

        // This method needs to be defined to get the core version
        private string GetYtFlowCoreVersion()
        {
            // Placeholder for the actual logic to retrieve the YtFlowCore version
            return "1.0.0"; // Example version
        }
    }
}
