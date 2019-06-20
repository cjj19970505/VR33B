using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VR33B.LineGraphic
{
    /// <summary>
    /// VR33BOxyLegendControl.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BOxyLegendControl : UserControl
    {
        public LineSeries BindingLineSeries
        {
            get
            {
                return DataContext as LineSeries;
            }
        }
        public VR33BOxyLegendControl()
        {
            InitializeComponent();
            
        }


    }
}
