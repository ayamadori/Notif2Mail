using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Notif2Mail
{
    // https://docs.microsoft.com/en-us/windows/uwp/launch-resume/create-and-register-a-background-task
    public sealed class ExampleBackgroundTask : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral; // Note: defined at class scope so we can mark it complete inside the OnCancel() callback if we choose to support cancellation
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();
            //
            // TODO: Insert code to start one or more asynchronous methods using the
            //       await keyword, for example:
            //
            // await ExampleMethodAsync();
            //

            switch (taskInstance.Task.Name)
            {
                case "UserNotificationChanged":
                    // Call your own method to process the new/removed notifications
                    // The next section of documentation discusses this code
                    await MyWearableHelpers.SyncNotifications();
                    break;
            }

            _deferral.Complete();
        }
    }

}
