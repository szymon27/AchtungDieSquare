using System.Windows.Media;

namespace Models
{
    public class Color : BaseModel
    {
        public byte A { get; set; }
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }

        static public SolidColorBrush ColorToSolidColorBrush(Color color)
        {
            return new SolidColorBrush(System.Windows.Media.Color.FromArgb(color.A,
                                                                           color.R,
                                                                           color.G,
                                                                           color.B));
        }
    }
}
