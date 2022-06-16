using System.Windows;

namespace Client.Utilities
{
    public class MsgBox
    {
        public static void Info(string content)
            => MessageBox.Show(content, "BookStore", MessageBoxButton.OK, MessageBoxImage.Information);

        public static void Error(string content)
            => MessageBox.Show(content, "BookStore", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
