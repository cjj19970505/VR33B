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

        public DateTime _ZeroAbscissaDateTime;

        public VR33BSampleValue[] _LoadedSampleValues;
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

        private DataRect _LatestPlotRect;

        public (DateTime GraphStartDateTime, DateTime GraphEndDateTime) GraphDateTimeRange
        {
            get
            {
                var plotRect = _LatestPlotRect;
                return (_AbscissaToDateTime(plotRect.XMin), _AbscissaToDateTime(plotRect.XMax));
            }
        }

        public VR33BSampleDataStorage _VR33BSampleDataStorage;
        public VR33BSampleDataStorage VR33BSampleDataStorage
        {
            get
            {
                return _VR33BSampleDataStorage;
            }

            set
            {
                if (_VR33BSampleDataStorage != null)
                {

                }
                _VR33BSampleDataStorage = value;
                _VR33BSampleDataStorage.Updated += _VR33BSampleDataStorage_Updated;

            }
        }

        public TimeSpan _UpdateInterval
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 100);
            }
        }

        private DateTime _LastPlotDateTime;
        private VR33BSampleValue _LatestSampleValue;

        private double _ReloadRangeAndLoadedRangeRatio = 0.3;
        private double _LoadedRangeAndDisplayRangeRatio = 2;
        private (DateTime Start, DateTime End) _LoadedRangeDateTime;
        private Task ReplotAsync()
        {
            TimeSpan displayRange = GraphDateTimeRange.GraphEndDateTime - GraphDateTimeRange.GraphStartDateTime;
            TimeSpan newHalfLoadedRange = new TimeSpan(0, 0, 0, 0, (int)(_LoadedRangeAndDisplayRangeRatio /2* displayRange.TotalMilliseconds));
            DateTime displayMidDateTime = GraphDateTimeRange.GraphStartDateTime.Add(new TimeSpan(0, 0, 0, 0, (int)(displayRange.TotalMilliseconds / 2)));
            DateTime newLoadStartDateTime = displayMidDateTime.Subtract(newHalfLoadedRange);
            DateTime newLoadEndDateTime = displayMidDateTime.Add(newHalfLoadedRange);
            _LoadedRangeDateTime = (newLoadStartDateTime, newLoadEndDateTime);
            return Task.Run(async () =>
            {
                var plotData = await VR33BSampleDataStorage.GetFromDateTimeRange(newLoadStartDateTime, newLoadEndDateTime);
                _LoadedSampleValues = plotData.ToArray();
                var axis_x = (from plotSampleValue in plotData
                              select plotSampleValue.AccelerometerValue.X).ToArray();
                var axis_y = (from plotSampleValue in plotData
                              select plotSampleValue.AccelerometerValue.Y).ToArray();
                var axis_z = (from plotSampleValue in plotData
                              select plotSampleValue.AccelerometerValue.Z).ToArray();
                var axis_datetime = plotData.ConvertAll((input) =>
                {
                    return _DateTimeToAbscissa(input.SampleDateTime);
                }).ToArray();
                await Dispatcher.InvokeAsync(() =>
                {
                    //XLineGraph.Plot(axis_datetime, axis_x);
                    //YLineGraph.Plot(axis_datetime, axis_y);
                    //ZLineGraph.Plot(axis_datetime, axis_z);
                    XMarkerGraph.PlotXY(axis_datetime, axis_x);
                });

            });
        }

        private async void _VR33BSampleDataStorage_Updated(object sender, VR33BSampleValue e)
        {
            
            if (e.SampleIndex == 0)
            {
                _ZeroAbscissaDateTime = e.SampleDateTime;
                _LastPlotDateTime = DateTime.Now;
                Dispatcher.Invoke(() =>
                {
                    XLineGraph.SetPlotRect(new DataRect(-1000, -40, 1000, 40));

                });
                _LatestSampleValue = e;
            }
            else if (_LatestSampleValue.SampleDateTime >= GraphDateTimeRange.GraphStartDateTime && _LatestSampleValue.SampleDateTime <= GraphDateTimeRange.GraphEndDateTime)
            {
                if(DateTime.Now - _LastPlotDateTime >= _UpdateInterval)
                {
                    _LastPlotDateTime = DateTime.Now;
                    double plotPosNorm = _DateTimeUnlerp(GraphDateTimeRange.GraphStartDateTime, GraphDateTimeRange.GraphEndDateTime, _LatestSampleValue.SampleDateTime);
                    plotPosNorm = 0.5;
                    double displayRangeInMs = (GraphDateTimeRange.GraphEndDateTime - GraphDateTimeRange.GraphStartDateTime).TotalMilliseconds;
                    double latestValueInMs = displayRangeInMs * plotPosNorm;
                    var displayStartDateTime = e.SampleDateTime.Subtract(new TimeSpan(0, 0, 0, 0, (int)latestValueInMs));
                    var startX = _DateTimeToAbscissa(displayStartDateTime);
                    _LatestSampleValue = e;
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var currDataRect = XLineGraph.PlotRect;
                        DataRect rect = new DataRect(startX, currDataRect.YMin, startX + currDataRect.Width, currDataRect.YMax);
                        XLineGraph.SetPlotRect(rect);
                        System.Diagnostics.Debug.WriteLine("RECT SET" + rect);
                    });
                    await ReplotAsync();
                }
                else
                {
                    _LatestSampleValue = e;
                }
            }
            else
            {
                _LatestSampleValue = e;
            }
        }

        private double _DateTimeUnlerp(DateTime begin, DateTime end, DateTime value)
        {
            return (value - begin).TotalMilliseconds / (end - begin).TotalMilliseconds;
        }

        private TimeSpan _TimeSpanLerp(TimeSpan begin, TimeSpan end, double value)
        {
            return new TimeSpan(0, 0, 0, 0, (int)((end - begin).TotalMilliseconds * value));
        }

        public VR33BGraph()
        {
            InitializeComponent();
            _ZeroAbscissaDateTime = DateTime.Now;


            //AddPoints();
            double[] x = new double[200];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[200];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;

            var initPlotRect = XLineGraph.PlotRect;

            //XLineGraph.StrokeThickness = 2;
            XMarkerGraph.Plot(x, x.Select(v => Math.Sin(v + 0 / 10.0)).ToArray());


            XLineGraph.PlotTransformChanged += XLineGraph_PlotTransformChanged;

        }

        private void XLineGraph_PlotTransformChanged(object sender, EventArgs e)
        {
            _LatestPlotRect = XLineGraph.PlotRect;
            //System.Diagnostics.Debug.WriteLine(XLineGraph.PlotRect);
            GraphBeginDateTimeLabel.Content = GraphDateTimeRange.GraphStartDateTime;
            GraphEndDateTimeLabel.Content = GraphDateTimeRange.GraphEndDateTime;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var xMid = _DateTimeToAbscissa(_LatestSampleValue.SampleDateTime);
            var plotRect = new DataRect(xMid - _LatestPlotRect.Width / 2, _LatestPlotRect.YMin, xMid + _LatestPlotRect.Width / 2, _LatestPlotRect.YMax);
            await Dispatcher.InvokeAsync(() =>
            {
                XLineGraph.SetPlotRect(plotRect);
            });
            await ReplotAsync();
            /*
            double[] x = new double[10000];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[10000];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;
            XLineGraph.Plot(x, x.Select(v => Math.Sin(v + 5 / 10.0)).ToArray());
            */

            //XLineGraph.Points.Clear();
        }


    }
}
