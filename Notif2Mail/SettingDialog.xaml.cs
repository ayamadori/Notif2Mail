using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Notif2Mail
{
    public sealed partial class SettingDialog : ContentDialog
    {
        public SettingDialog()
        {
            this.InitializeComponent();

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
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool ssl = (CheckSSL.IsChecked == true) ? true : false;
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            MyWearableHelpers.SaveSettings(AddressBox.Text, ServerBox.Text, int.Parse(PortBox.Text), ssl, UsernameBox.Text, PassBox.Password);
        }
    }
}
