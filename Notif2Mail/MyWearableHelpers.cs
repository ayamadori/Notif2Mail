using LightBuzz.SMTP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Email;
using Windows.Data.Xml.Dom;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;
using Windows.UI.Popups;

// https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/notification-listener

namespace Notif2Mail
{
    public class MyWearableHelpers
    {
        private const string RESOURCE = "Notif2Mail_account";

        public static async void RegisterBackgroundTask()
        {
            // TODO: Request/check Listener access via UserNotificationListener.Current.RequestAccessAsync

            // Get the listener
            UserNotificationListener listener = UserNotificationListener.Current;

            // And request access to the user's notifications (must be called from UI thread)
            UserNotificationListenerAccessStatus accessStatus = await listener.RequestAccessAsync();

            switch (accessStatus)
            {
                // This means the user has granted access.
                case UserNotificationListenerAccessStatus.Allowed:

                    // Yay! Proceed as normal
                    break;

                // This means the user has denied access.
                // Any further calls to RequestAccessAsync will instantly
                // return Denied. The user must go to the Windows settings
                // and manually allow access.
                case UserNotificationListenerAccessStatus.Denied:

                    // Show UI explaining that listener features will not
                    // work until user allows access.
                    var dlg = new MessageDialog("Access request is denied. Go to the Windows settings and manually allow access.");
                    await dlg.ShowAsync();
                    break;

                // This means the user closed the prompt without
                // selecting either allow or deny. Further calls to
                // RequestAccessAsync will show the dialog again.
                case UserNotificationListenerAccessStatus.Unspecified:

                    // Show UI that allows the user to bring up the prompt again
                    break;
            }

            // TODO: Request/check background task access via BackgroundExecutionManager.RequestAccessAsync
            // https://docs.microsoft.com/en-us/windows/uwp/launch-resume/run-a-background-task-on-a-timer-
            var requestStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (requestStatus != BackgroundAccessStatus.AlwaysAllowed)
            {
                // Depending on the value of requestStatus, provide an appropriate response
                // such as notifying the user which functionality won't work as expected
                var dlg = new MessageDialog("Background access is not \"always allowed\". Go to the Windows settings and manually allow access");
                await dlg.ShowAsync();
            }

            // If background task isn't registered yet
            if (!BackgroundTaskRegistration.AllTasks.Any(i => i.Value.Name.Equals("UserNotificationChanged")))
            {
                // Specify the background task
                var builder = new BackgroundTaskBuilder()
                {
                    Name = "UserNotificationChanged"
                };

                // Set the trigger for Listener, listening to Toast Notifications
                builder.SetTrigger(new UserNotificationChangedTrigger(NotificationKinds.Toast));

                // Register the task
                builder.Register();
            }
        }

        public static async Task SyncNotifications()
        {
            // Get the listener
            UserNotificationListener listener = UserNotificationListener.Current;

            // Get all the current notifications from the platform
            IReadOnlyList<UserNotification> userNotifications = await listener.GetNotificationsAsync(NotificationKinds.Toast);

            // Obtain the notifications that our wearable currently has displayed
            List<uint> wearableNotificationIds = GetNotificationsOnWearable(userNotifications);

            // For each notification in the platform
            foreach (UserNotification userNotification in userNotifications)
            {
                // If we've already displayed this notification
                if (wearableNotificationIds.Contains(userNotification.Id))
                {
                }

                // Othwerise it's a new notification
                else
                {
                    // Display it on the Wearable
                    SendNotificationToWearable(userNotification);
                }
            }
        }

        private static async void SendNotificationToWearable(UserNotification userNotification)
        {
            string titleText = "";
            string bodyText = "";

            // Get the app name
            String appName = userNotification.AppInfo.DisplayInfo.DisplayName;

            // Get the toast binding, if present
            NotificationBinding toastBinding = userNotification.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);

            if (toastBinding != null)
            {
                // And then get the text elements from the toast binding
                IReadOnlyList<AdaptiveNotificationText> textElements = toastBinding.GetTextElements();

                // Treat the first text element as the title text
                titleText = textElements.FirstOrDefault()?.Text;

                // We'll treat all subsequent text elements as body text,
                // joining them together via newlines.
                bodyText = string.Join("\n", textElements.Skip(1).Select(t => t.Text));
            }

            // Load settings
            string settings = LoadSettings();
            string[] setting;
            if (settings != null)
                setting = settings.Split(';');
            else
                return;
            bool ssl = (setting[3] == "1") ? true : false;

            // https://github.com/LightBuzz/smtp-winrt
            using (SmtpClient client = new SmtpClient(setting[1], Int32.Parse(setting[2]), ssl, setting[4], setting[5]))
            {
                EmailMessage emailMessage = new EmailMessage();

                emailMessage.To.Add(new EmailRecipient(setting[0]));
                emailMessage.Subject = $"[Notif2Mail] <{appName}> {titleText}";
                emailMessage.Body = bodyText;

                await client.SendMailAsync(emailMessage);
            }
        }

        private static List<uint> GetNotificationsOnWearable(IReadOnlyList<UserNotification> userNotifications)
        {
            List<uint> list = new List<uint>();

            // Load notifications
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            // Read data from a simple setting
            string notifications = roamingSettings.Values["Notifications"] as string;
            if (notifications != null)
            {
                string[] notification = notifications.Split(';');
                for (int i = 0; i < notification.Length; i++)
                {
                    if (notification[i].Length > 0)
                        list.Add(uint.Parse(notification[i]));
                }
            }

            // Save notifications
            notifications = "";
            foreach (UserNotification userNotification in userNotifications)
                notifications += $"{userNotification.Id};";
            roamingSettings.Values["Notifications"] = notifications;

            return list;
        }

        public static void SaveSettings(string recipient, string server, int port, bool ssl, string username, string password)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            string checkSSL = ssl ? "1" : "0";
            roamingSettings.Values["Settings"] = $"{recipient};{server};{port};{checkSSL}";
            SaveCredential(username, password);
        }

        private static async void SaveCredential(string username, string password)
        {
            if (username == "" || password == "")
            {
                var dlg = new MessageDialog("All fields are required when saving a credential.");
                await dlg.ShowAsync();
            }
            else
            {
                // Add a credential to PasswordVault with provided resource, user name, and password.
                // Replaces any existing credential with the same resource and user name.
                var vault = new PasswordVault();

                try
                {
                    // Remove already added account
                    var creds = vault.FindAllByResource(RESOURCE);
                    if (creds.Count > 0)
                        foreach (var c in creds) vault.Remove(c);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ERROR: " + ex.ToString());
                }
                finally
                {
                    // Save credential
                    var cred = new PasswordCredential(RESOURCE, username, password);
                    vault.Add(cred);

                    //var dlg = new MessageDialog("Credential saved successfully. Resource: " + cred.Resource + " Username: " + cred.UserName + " Password: " + cred.Password);
                    var dlg = new MessageDialog("Credential saved successfully.");
                    await dlg.ShowAsync();
                }
            }
        }

        public static string LoadSettings()
        {
            // Load settings
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            // Read data from a simple setting
            string setting = roamingSettings.Values["Settings"] as string;
            string cred = LoadCredential();
            if (setting == null || cred == null) return null;

            return $"{setting};{cred}";
        }

        private static string LoadCredential()
        {
            var vault = new PasswordVault();
            IReadOnlyList<PasswordCredential> creds = null;

            // The credential retrieval functions raise an "Element not found"
            // exception if there were no matches.
            try
            {
                // Resource is provided but no user name: Use findAllByResource().
                creds = vault.FindAllByResource(RESOURCE);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ERROR: " + ex.ToString());
                return null;
            }

            // Display the results. Note that the password field is initially blank.
            // You must call retrievePassword() to get the password.

            if (creds == null || creds.Count == 0)
            {
                return null;
            }
            else
            {
                creds[0].RetrievePassword();
                //Debug.WriteLine("Credentials found: " + creds[0].Resource + ": " + creds[0].UserName + ": " + creds[0].Password);
                return $"{creds[0].UserName};{creds[0].Password}";
            }
        }

        public static void ShowToast()
        {
            // Show a local toast to launch app again
            // https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/send-local-toast
            // Create and show the toast notification
            string toastContent = @"<toast>
                                     <visual>
                                      <binding template = ""ToastGeneric"">
                                       <text> App suspended </text>
                                       <text> Launch the app again to check the incoming notifications.</text>
                                      </binding>
                                     </visual>
                                    </toast>";
            // Load the string into an XmlDocument
            XmlDocument toastXML = new XmlDocument();
            toastXML.LoadXml(toastContent);
            var toast = new ToastNotification(toastXML);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
