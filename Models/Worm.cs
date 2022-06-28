using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Models
{
    public enum Direction
    {
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4,
    }
    public class Worm
    {
        private const int size = 5;
        private WormPart head;

        public List<WormPart> WormParts { get; set; }
        public Color Color { get; set; }
        public Direction Direction { get; set; }
        public int Id { get; set; }
        public bool isAlive { get; set; }
        public WormMove GetWormMove()
        {
            return new WormMove
            {
                X = (int)head.Position.X,
                Y = (int)head.Position.Y,
                Color = Color
            };
        }
        public Worm(WormMove wm)
        {
            Random r = new Random();
            this.Color = wm.Color;
            this.isAlive = true;
            this.WormParts = new List<WormPart>();
            this.Direction = (Direction)r.Next(1, 5);
            this.WormParts.Add(new WormPart()
            {
                Position = new Point(wm.X, wm.Y),
                isHead = true,
                Rect = new Rect()
                {
                    Width = size,
                    Height = size,
                }
            });
            head = this.WormParts.Last();
        }

        public WormPart expandWorm()
        {
            double nextX = head.Position.X;
            double nextY = head.Position.Y;

            switch (Direction)
            {
                case Direction.Left:
                    nextX -= size;
                    break;
                case Direction.Right:
                    nextX += size;
                    break;
                case Direction.Up:
                    nextY -= size;
                    break;
                case Direction.Down:
                    nextY += size;
                    break;
            }
            head.isHead = false;
            WormParts.Add(new WormPart()
            {
                Position = new Point(nextX, nextY),
                Rect = new Rect()
                {
                    Width = size,
                    Height = size,
                },
                isHead = true
            });
            head = WormParts.Last();
            return head;
        }
    }
}
