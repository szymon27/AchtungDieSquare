using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace Models
{
    public class WormPart
    {
        public Rect Rect { get; set; }
        public Point Position { get; set; }
        public bool isHead { get; set; }
    }
}
