using Client.Utilities;
using Models;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.ViewModels
{
    public class GameViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;

        public Canvas _canvas;
        public Canvas Canvas
        {
            get => _canvas;
            set
            {
                _canvas = value;
                OnPropertyChanged(nameof(Canvas));
            }
        }

        public GameViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;

            App.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                Canvas canvas = new Canvas();
                canvas.Width = 300;
                canvas.Height = 300;
                canvas.Background = new SolidColorBrush(Colors.Red);
                canvas.Visibility = System.Windows.Visibility.Visible;
                canvas.Focusable = true;
                canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                canvas.KeyDown += Canvas_KeyDown;
                Canvas = canvas;
            });
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            ((Canvas)sender).Focus();
            Direction? direction = null;
            switch(e.Key)
            {
                case Key.W: direction = Direction.Up; break;
                case Key.S: direction = Direction.Down; break;
                case Key.A: direction = Direction.Left; break;
                case Key.D: direction = Direction.Right; break;
            }
            if (direction != null)
                Console.WriteLine(direction.ToString());
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).Focus();
        }
    }
}
