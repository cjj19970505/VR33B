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
        List<VR33BSampleValue> _NewSampleValues;
        private VR33BTerminal _VR33BTerminal;

        public List<VR33BSampleValue> _OnGraphSampleValues;
        
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            set
            {
                if (_VR33BTerminal != null)
                {
                    _VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                    _VR33BTerminal.OnVR33BSampleStarted -= _VR33BTerminal_OnVR33BSampleStarted;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
            }
        }
        private bool _FirstSampleValue = false;
        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, EventArgs e)
        {
            _FirstSampleValue = true;
            Dispatcher.Invoke(() =>
            {
                XLineGraph.Points.Clear();
            });
        }

        public TimeSpan _GraphUpdateTimeSpan
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 1000);
            }
        }

        private void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            if(_FirstSampleValue)
            {
                _ZeroAbscissaDateTime = e.SampleDateTime;
                _FirstSampleValue = false;
            }
            if (_NewSampleValues.Count > 0 && e.SampleDateTime - _NewSampleValues[0].SampleDateTime > _GraphUpdateTimeSpan)
            {
                for (int i = 0; i < _NewSampleValues.Count; i++)
                {
                    AddSampleValue(_NewSampleValues[i]);
                }
                _NewSampleValues.Clear();
            }
            else
            {
                _NewSampleValues.Add(e);
            }
        }

        public ObservableCollection<VR33BSampleValue> SampleValueSource
        {
            set
            {
                _SampleValueSource = new ObservableCollection<VR33BSampleValue>();
            }
        }

        public DateTime _ZeroAbscissaDateTime;
        private double _DateTimeToAbscissa(DateTime dateTime)
        {
            return (dateTime - _ZeroAbscissaDateTime).TotalMilliseconds;
        }

        private DateTime _AbscissaToDateTime(double abscissa)
        {
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, (int)Math.Floor(abscissa));
            var finalDateTime = _ZeroAbscissaDateTime;
            return finalDateTime.Add(timeSpan);
        }

        public void AddSampleValue(VR33BSampleValue sampleValue)
        {
            _OnGraphSampleValues.Add(sampleValue);
            var x = _OnGraphSampleValues.ConvertAll((input) =>
            {
                return _DateTimeToAbscissa(input.SampleDateTime);
            }).ToArray();
            var y = _OnGraphSampleValues.ConvertAll((input) =>
            {
                return input.AccelerometerValue.X;
            }).ToArray();
            Dispatcher.Invoke(() =>
            {
                XLineGraph.Plot(x, y);
            });
        }

        public (DateTime GraphStartDateTime, DateTime GraphEndDateTime) GraphDateTimeRange
        {
            get
            {
                var plotRect = XLineGraph.PlotRect;
                return (_AbscissaToDateTime(plotRect.XMin), _AbscissaToDateTime(plotRect.XMax));
            }
        }

        public VR33BGraph()
        {
            InitializeComponent();
            _ZeroAbscissaDateTime = DateTime.Now;
            _NewSampleValues = new List<VR33BSampleValue>();
            _OnGraphSampleValues = new List<VR33BSampleValue>();
            //AddPoints();
            double[] x = new double[200];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[200];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;

            XLineGraph.StrokeThickness = 2;
            XLineGraph.Plot(x, x.Select(v => Math.Sin(v + 0 / 10.0)).ToArray());

            
            XLineGraph.PlotTransformChanged += XLineGraph_PlotTransformChanged;

        }

        private void XLineGraph_PlotTransformChanged(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(XLineGraph.PlotRect);
            GraphBeginDateTimeLabel.Content = GraphDateTimeRange.GraphStartDateTime;
            GraphEndDateTimeLabel.Content = GraphDateTimeRange.GraphEndDateTime;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            double[] x = new double[10000];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[10000];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;
            XLineGraph.Plot(x, x.Select(v => Math.Sin(v + 5 / 10.0)).ToArray());
            
            //XLineGraph.Points.Clear();
        }
    }
}
