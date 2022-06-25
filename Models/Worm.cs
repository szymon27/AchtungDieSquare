using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

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

        public List<WormPart> WormParts { get; set; }
        public SolidColorBrush color { get; set; }
        public Direction Direction { get; set; }
        public int Id { get; set; }
        public bool isAlive { get; set; }

        public WormPart setUp()
        {
            Random r = new Random();
            this.isAlive = true;
            this.WormParts = new List<WormPart>();
            this.Direction = (Direction)r.Next(1, 5);
            this.WormParts.Add(new WormPart()
            {
                Position = new Point(r.Next(4, 57) * size, r.Next(4, 57) * size),
                isHead = true,
                UIElement = new Rectangle()
                {
                    Width = size,
                    Height = size,
                    Fill = color
                }
            });
            return this.WormParts.Last();
        }

        public WormPart expandWorm()
        {
            WormPart head = WormParts.Last();
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
                UIElement = new Rectangle()
                {
                    Width = size,
                    Height = size,
                    Fill = color
                },
                isHead = true
            });
            return WormParts.Last();
        }
    }
}
