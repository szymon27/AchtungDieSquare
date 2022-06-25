using System.Windows;
using System.Windows.Input;

namespace Client.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    //if (worm.Direction != Direction.Down)
                    //{
                    //    worm.Direction = Direction.Up;
                    //}
                    break;
                case Key.Down:
                    //if (worm.Direction != Direction.Up)
                    //{
                    //    worm.Direction = Direction.Down;
                    //}
                    break;
                case Key.Left:
                    //if (worm.Direction != Direction.Right)
                    //{
                    //    worm.Direction = Direction.Left;
                    //}
                    break;
                case Key.Right:
                    //if (worm.Direction != Direction.Left)
                    //{
                    //    worm.Direction = Direction.Right;
                    //}
                    break;
            }
        }
    }
}
