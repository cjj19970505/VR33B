using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// VR33BGraph.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BGraph : UserControl
    {
        ObservableCollection<VR33BSampleValue> _SampleValueSource;
        public ObservableCollection<VR33BSampleValue> SampleValueSource
        {
            set
            {
                _SampleValueSource = new ObservableCollection<VR33BSampleValue>();
            }
        }

        public VR33BGraph()
        {
            InitializeComponent();

            double[] x = new double[200];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[200];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;
            for (int i = 0; i < 3; i++)
            {
                var lg = new LineGraph();
                var cg = new CircleMarkerGraph();

                Lines.Children.Add(lg);
                lg.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                if (i == 0)
                {
                    lg.Description = cg.Description = "Axis-X";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Red);
                }
                if (i == 1)
                {
                    lg.Description = cg.Description = "Axis-Y";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Green);
                }
                if (i == 2)
                {
                    lg.Description = cg.Description = "Axis-Z";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Blue);
                }
                lg.StrokeThickness = 2;
                lg.Plot(x, x.Select(v => Math.Sin(v + i / 10.0)).ToArray());

                Lines.Children.Add(cg);
                

                //cg.PlotSize (x, x.Select(v => Math.Sin(v + i / 10.0)), size);
                cg.PlotXY(x, x.Select(v => Math.Sin(v + i / 10.0)));
                cg.Size = size;
                //cg.Size = size.ConvertAll(v => { return 100.0; }).ToArray();
                //cg.MarkersBatchSize = 2;
            }
        }
    }
}
