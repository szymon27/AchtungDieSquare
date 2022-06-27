﻿using Client.Utilities;
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
        public List<Worm> worms = new List<Worm>();
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
            _client.CountDownEvent += (sec) =>
            {
                CountDown = $"The game will start in {sec}";
            };
            _client.StartRoundEvent += () => CountDown = "Start!";
            _client.StartingPointsEvent += (wp) =>
            {
                Worm worm = new Worm(wp);
                worms.Add(worm);

                WormPart start = worm.WormParts.Last();
                this.Canvas.Visibility = Visibility.Collapsed;

                    UIElement uielement = new Rectangle()
                    {
                        Width = 5,
                        Height = 5,
                        Fill = worm.color
                    };
                    this.Canvas.Children.Add(uielement);
                    Canvas.SetTop(uielement, start.Position.Y);
                    Canvas.SetLeft(uielement, start.Position.X);

                this.Canvas.Visibility = Visibility.Visible;
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
                Console.WriteLine(direction.ToString());
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ((Canvas)sender).Focus();
        }
    }
}
