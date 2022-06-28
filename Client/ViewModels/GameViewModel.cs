using Client.Utilities;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Client.ViewModels
{
    public class GameViewModel : BaseViewModel
    {
        private Client _client;
        private EventAggregator _eventAggregator;
        private Canvas _canvas;
        private string _countDown;

        public Canvas Canvas
        {
            get => _canvas;
            set
            {
                _canvas = value;
                OnPropertyChanged(nameof(Canvas));
            }
        }
        public string CountDown
        {
            get => _countDown;
            set
            {
                _countDown = value;
                OnPropertyChanged(nameof(CountDown));
            }
        }
        public GameViewModel(Client client, EventAggregator eventAggregator)
        {
            _client = client;
            _eventAggregator = eventAggregator;

            _client.CountDownEvent += (sec) =>
            {
                CountDown = $"The round will start in {sec}";
            };

            _client.StartRoundEvent += () => CountDown = "Start!";
            _client.NextMoveEvent += (wm) =>
            {           
                App.Current.Dispatcher.BeginInvoke((Action)delegate
                {                  
                    UIElement uielement = new Rectangle()
                    {
                        Width = 5,
                        Height = 5,
                        Fill = Models.Color.ColorToSolidColorBrush(wm.Color)
                    };
                    this.Canvas.Children.Add(uielement);
                    Canvas.SetTop(uielement, wm.Y);
                    Canvas.SetLeft(uielement, wm.X);
                });

            };
            _client.ClearBoardEvent += () => 
            {
                App.Current.Dispatcher.BeginInvoke((Action)delegate
                {
                    if(Canvas != null)
                        Canvas.Children.Clear();
                    Canvas canvas = new Canvas();
                    canvas.Width = 300;
                    canvas.Height = 300;
                    canvas.Background = new SolidColorBrush(Colors.LightGray);
                    canvas.Visibility = System.Windows.Visibility.Visible;
                    canvas.Focusable = true;
                    canvas.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
                    canvas.KeyDown += Canvas_KeyDown;
                    Canvas = canvas;
                    Canvas.Focus();
                });
            };
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
                _client.ChangeDirection(direction.Value);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).Focus();
        }
    }
}
