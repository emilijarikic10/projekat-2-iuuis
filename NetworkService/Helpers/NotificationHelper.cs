using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Notification.Wpf;
using System.Windows.Media;

namespace NetworkService.Helpers
{
    public static class NotificationHelper
    {
        private const string AreaName = "WindowNotificationArea";
        private static readonly NotificationManager notificationManager = new NotificationManager();

        public static void ShowSuccess(string title, string message)
        {
            Show(title, message, NotificationType.Success, Colors.SeaGreen);
        }

        public static void ShowWarning(string title, string message)
        {
            Show(title, message, NotificationType.Warning, Colors.DarkOrange);
        }

        public static void ShowError(string title, string message)
        {
            Show(title, message, NotificationType.Error, Colors.Crimson);
        }

        public static void ShowInfo(string title, string message)
        {
            Show(title, message, NotificationType.Information, Colors.SteelBlue);
        }

        private static void Show(string title, string message, NotificationType type, Color background)
        {
            NotificationContent content = new NotificationContent
            {
                Title = title,
                Message = message,
                Type = type,
                TrimType = NotificationTextTrimType.AttachIfMoreRows,
                RowsCount = 2,
                CloseOnClick = true,
                Background = new SolidColorBrush(background),
                Foreground = new SolidColorBrush(Colors.White)
            };

            notificationManager.Show(content, AreaName);
        }
    }
}
