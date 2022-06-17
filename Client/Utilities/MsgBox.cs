using System.Windows;

namespace Client.Utilities
{
    public class MsgBox
    {
        private static string title = "Achtung Die Square";

        public static void Info(string content)
            => MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Error(string content)
            => MessageBox.Show(content, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
