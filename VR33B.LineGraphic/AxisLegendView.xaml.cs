using ColorPickerWPF;
using InteractiveDataDisplay.WPF;
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
    /// AxisLegendView.xaml 的交互逻辑
    /// </summary>
    public partial class AxisLegendView : UserControl
    {
        public AxisLegendView()
        {
            InitializeComponent();
        }

        private void ColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("FYCJ");
            bool ok = ColorPickerWindow.ShowDialog(out Color color);
            if (ok && DataContext is LineGraph)
            {
                (DataContext as LineGraph).Stroke = new SolidColorBrush(color);
            }
            
        }

        private void AddPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LineGraph)
            {
                var lineGraph = (DataContext as LineGraph);
                lineGraph.Points.Add(new Point(1, 1));
                lineGraph.SetPlotRect(new DataRect(-10, -10, 10, 10));
            }
        }
    }

    
}
