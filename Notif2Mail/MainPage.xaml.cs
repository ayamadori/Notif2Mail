using LightBuzz.SMTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.System;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Notif2Mail
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string settings = MyWearableHelpers.LoadSettings();
            if (settings != null)
            {
                string[] setting = settings.Split(';');
                AddressBox.Text = setting[0];
                ServerBox.Text = setting[1];
                PortBox.Text = setting[2];
                CheckSSL.IsChecked = (setting[3] == "1") ? true : false;
                UsernameBox.Text = setting[4];
                PassBox.Password = setting[5];
            }

            MyWearableHelpers.RegisterBackgroundTask();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool ssl = (CheckSSL.IsChecked == true) ? true : false;
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            MyWearableHelpers.SaveSettings(AddressBox.Text, ServerBox.Text, Int32.Parse(PortBox.Text), ssl, UsernameBox.Text, PassBox.Password);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var uriReview = new Uri($"ms-windows-store:REVIEW?PFN={Package.Current.Id.FamilyName}");
            var success = await Launcher.LaunchUriAsync(uriReview);
        }
    }
}
