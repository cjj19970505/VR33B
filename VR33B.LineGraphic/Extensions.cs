using OxyPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace VR33B.LineGraphic
{
    public static class Extensions
    {
        internal static Color ToColor(this OxyColor self)
        {
            return Color.FromArgb(self.A, self.R, self.G, self.B);
        }
        internal static OxyColor ToOxyColor(this Color self)
        {
            return OxyColor.FromArgb(self.A, self.R, self.G, self.B);
        }
    }
}
