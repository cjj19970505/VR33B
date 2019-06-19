using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

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
        private VR33BTerminal _VR33BTerminal;
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
                    _VR33BTerminal.VR33BSampleDataStorage.Updated -= _VR33BSampleDataStorage_Updated;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.VR33BSampleDataStorage.Updated += _VR33BSampleDataStorage_Updated;
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

        private double _ReloadRangeAndLoadedRangeRatio = 0.1;
        private double _LoadedRangeAndDisplayRangeRatio = 2;
        private double _MinDisplayRangeAndLoadedRangeRatio = 0.1;
        private (DateTime Start, DateTime End) _LoadedRangeDateTime;
        /// <summary>
        /// 用来标记一次Replot请求
        /// 由于整个绘制过程包含 取点 绘制 两个耗时较长的阻塞过程，因此若在取点结束之后又有新的replot请求，则就直接放弃这次绘制，转而处理新的Replot请求
        /// </summary>
        private Guid _LatestRepaintGuid;
        /// <summary>
        /// 在显示范围较大时载入太多的Marker时点也点不准，而且使InteractiveDisplay的交互速度下降
        /// 因此在loaded范围太大时不显示marker
        /// </summary>
        private int _MaxMarkerCountInLoadedRangePerAxis = 200;
        /// <summary>
        /// 由于在Tracking模式下更新频繁，若一次性plot大量数据会造成很大的性能损失，因此进行降采样
        /// </summary>
        private int _MaxLoadedSampleCountInTracking = 80;

        bool _TrackingModeOn = false;
        public bool TrackingModeOn
        {
            get
            {
                return _TrackingModeOn;
            }
            set
            {
                _TrackingModeOn = value;
                if(!_TrackingModeOn)
                {
                    _TrackingModeReplotTimer.Stop();
                }
                else
                {
                    _TrackingModeReplotTimer.Interval = _UpdateInterval;
                    _TrackingModeReplotTimer.Start();
                }
            }
        }

        private void _TrackingModeReplotTimer_Tick(object sender, EventArgs e)
        {
            if(!TrackingModeOn)
            {
                return;
            }
            (bool plotMarkers, DataRect? plotRect, double[] axis_x, double[] axis_y, double[] axis_z, double[] axis_datetime) latestReplotRequest = (false, null, null, null, null, null);
            lock (_ReplotRequestListLock)
            {
                if(_ReplotRequestList.Count > 0)
                {
                    latestReplotRequest = _ReplotRequestList.Last();
                    _ReplotRequestList.Clear();
                    
                }
                else
                {
                    return;
                }
            }
            var axis_datetime = latestReplotRequest.axis_datetime;
            var axis_x = latestReplotRequest.axis_x;
            var axis_y = latestReplotRequest.axis_y;
            var axis_z = latestReplotRequest.axis_z;
            var plotRect = latestReplotRequest.plotRect;
            var plotMarkers = latestReplotRequest.plotMarkers;
            var plotCount = axis_datetime.Length;
            if (plotCount == 0)
            {
                return;
            }
            XLineGraph.Plot(axis_datetime, axis_x);
            YLineGraph.Plot(axis_datetime, axis_y);
            ZLineGraph.Plot(axis_datetime, axis_z);

            if (plotMarkers && plotCount <= _MaxMarkerCountInLoadedRangePerAxis)
            {
                XMarkerGraph.PlotXY(axis_datetime, axis_x);
                YMarkerGraph.PlotXY(axis_datetime, axis_y);
                ZMarkerGraph.PlotXY(axis_datetime, axis_z);
            }
            else
            {
                XMarkerGraph.PlotXY(new double[0], new double[0]);
                YMarkerGraph.PlotXY(new double[0], new double[0]);
                ZMarkerGraph.PlotXY(new double[0], new double[0]);
            }
            if (plotRect != null)
            {
                XLineGraph.SetPlotRect((DataRect)plotRect);
            }
        }

        private object _ReplotRequestListLock;
        private List<(bool plotMarkers, DataRect? plotRect, double[] axis_x, double[] axis_y, double[] axis_z, double[] axis_datetime)> _ReplotRequestList;
        private bool _ReplotRequest = false;
        private DispatcherTimer _TrackingModeReplotTimer;
        private Task ReplotAsync(bool plotMarkers, bool trackingPlot, DataRect? plotRect = null)
        {
            var repaintGuid = _LatestRepaintGuid = Guid.NewGuid();
            TimeSpan displayRange = GraphDateTimeRange.GraphEndDateTime - GraphDateTimeRange.GraphStartDateTime;
            TimeSpan newHalfLoadedRange = new TimeSpan(0, 0, 0, 0, (int)(_LoadedRangeAndDisplayRangeRatio / 2 * displayRange.TotalMilliseconds));
            DateTime displayMidDateTime = GraphDateTimeRange.GraphStartDateTime.Add(new TimeSpan(0, 0, 0, 0, (int)(displayRange.TotalMilliseconds / 2)));
            DateTime newLoadStartDateTime = displayMidDateTime.Subtract(newHalfLoadedRange);
            DateTime newLoadEndDateTime = displayMidDateTime.Add(newHalfLoadedRange);
            _LoadedRangeDateTime = (newLoadStartDateTime, newLoadEndDateTime);
            return Task.Run(async () =>
            {
                DateTime beforeLookup = DateTime.Now;

                var plotData = await VR33BTerminal.VR33BSampleDataStorage.GetFromDateTimeRange(newLoadStartDateTime, newLoadEndDateTime);
                _LoadedSampleValues = plotData.ToArray();
                var plotCount = plotData.Count;
                if (TrackingModeOn)
                {
                    if (plotCount >= 2 * _MaxLoadedSampleCountInTracking)
                    {
                        int step = plotCount / _MaxLoadedSampleCountInTracking;
                        plotData = (from sampleValue in plotData
                                    where sampleValue.SampleIndex % step == 0
                                    select sampleValue).ToList();
                        plotCount = plotData.Count;
                    }
                }
                var axis_x = (from plotSampleValue in plotData
                              select (double)plotSampleValue.AccelerometerValue.X).ToArray();
                var axis_y = (from plotSampleValue in plotData
                              select (double)plotSampleValue.AccelerometerValue.Y).ToArray();
                var axis_z = (from plotSampleValue in plotData
                              select (double)plotSampleValue.AccelerometerValue.Z).ToArray();
                var axis_datetime = plotData.ConvertAll((input) =>
                {
                    return _DateTimeToAbscissa(input.SampleDateTime);
                }).ToArray();
                if(trackingPlot)
                {
                    lock (_ReplotRequestListLock)
                    {
                        _ReplotRequestList.Add((plotMarkers, plotRect, axis_x, axis_y, axis_z, axis_datetime));
                    }
                    _ReplotRequest = true;
                }
                else
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        replotFunc(plotMarkers, plotRect, axis_x, axis_y, axis_z, axis_datetime);
                    });
                }
                
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
                    XLineGraph.SetPlotRect(new DataRect(-1000, -1, 1000, 1));

                });
                _LatestSampleValue = e;
            }
            else
            {
                if (DateTime.Now - _LastPlotDateTime >= _UpdateInterval)
                {
                    bool latestSampleValueOnScreen = _LatestSampleValue.SampleDateTime >= GraphDateTimeRange.GraphStartDateTime && _LatestSampleValue.SampleDateTime <= GraphDateTimeRange.GraphEndDateTime;
                    bool latestSampleValueOnLoadedRange = _LatestSampleValue.SampleDateTime >= _LoadedRangeDateTime.Start && _LatestSampleValue.SampleDateTime <= _LoadedRangeDateTime.End;
                    DataRect rect = _LatestPlotRect;

                    if (TrackingModeOn)
                    {
                        double plotPosNorm = 0.5;
                        if (latestSampleValueOnScreen)
                        {
                            plotPosNorm = _DateTimeUnlerp(GraphDateTimeRange.GraphStartDateTime, GraphDateTimeRange.GraphEndDateTime, _LatestSampleValue.SampleDateTime);
                        }
                        plotPosNorm = 0.5;

                        double displayRangeInMs = (GraphDateTimeRange.GraphEndDateTime - GraphDateTimeRange.GraphStartDateTime).TotalMilliseconds;
                        double latestValueInMs = displayRangeInMs * plotPosNorm;
                        var displayStartDateTime = e.SampleDateTime.Subtract(new TimeSpan(0, 0, 0, 0, (int)latestValueInMs));
                        var startX = _DateTimeToAbscissa(displayStartDateTime);
                        _LatestSampleValue = e;

                        var currDataRect = _LatestPlotRect;
                        rect = new DataRect(startX, currDataRect.YMin, startX + currDataRect.Width, currDataRect.YMax);
                    }
                    _LastPlotDateTime = DateTime.Now;
                    if (TrackingModeOn)
                    {
                        await ReplotAsync(false, true, rect);
                        System.Diagnostics.Debug.WriteLine("REPAIN_POST");
                    }
                    else if (latestSampleValueOnLoadedRange)
                    {
                        await ReplotAsync(true, false);
                    }


                    _LatestSampleValue = e;


                }
                else
                {
                    _LatestSampleValue = e;
                }
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
            this.DataContext = this;
            _ZeroAbscissaDateTime = DateTime.Now;
            XLineGraph.PlotTransformChanged += XLineGraph_PlotTransformChanged;
            _ReplotRequestList = new List<(bool plotMarkers, DataRect? plotRect, double[] axis_x, double[] axis_y, double[] axis_z, double[] axis_datetime)>();
            _ReplotRequestListLock = new object();
            _TrackingModeReplotTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher);
            
            _TrackingModeReplotTimer.Tick += _TrackingModeReplotTimer_Tick;
            //AddPoints();
            double[] x = new double[200];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);

            var initPlotRect = XLineGraph.PlotRect;

            //XLineGraph.StrokeThickness = 2;
            //ZMarkerGraph.PlotXY(x, x.Select(v => Math.Sin(v + 0 / 10.0)).ToArray());



        }

        private async void XLineGraph_PlotTransformChanged(object sender, EventArgs e)
        {

            await Dispatcher.InvokeAsync(() =>
            {
                _LatestPlotRect = XLineGraph.PlotRect;
                //System.Diagnostics.Debug.WriteLine(XLineGraph.PlotRect);
                GraphBeginDateTimeLabel.Content = GraphDateTimeRange.GraphStartDateTime;
                GraphEndDateTimeLabel.Content = GraphDateTimeRange.GraphEndDateTime;
            });

            if (!TrackingModeOn || !VR33BTerminal.Sampling)
            {
                var reloadBorderLeftFromloadBorderLeftTimeSpan = new TimeSpan(0, 0, 0, 0, (int)((_LoadedRangeDateTime.End - _LoadedRangeDateTime.Start).TotalMilliseconds * _ReloadRangeAndLoadedRangeRatio));
                var reloadBorderLeftDateTime = _LoadedRangeDateTime.Start.Add(reloadBorderLeftFromloadBorderLeftTimeSpan);
                var reloadBorderRightToLoadBorderRightTimeSpan = new TimeSpan(0, 0, 0, 0, (int)((_LoadedRangeDateTime.End - _LoadedRangeDateTime.Start).TotalMilliseconds * (1 - _ReloadRangeAndLoadedRangeRatio)));
                var reloadBorderRightDateTime = _LoadedRangeDateTime.End.Subtract(reloadBorderRightToLoadBorderRightTimeSpan);

                if (_AbscissaToDateTime(_LatestPlotRect.XMin) < _LoadedRangeDateTime.Start || _AbscissaToDateTime(_LatestPlotRect.XMax) > _LoadedRangeDateTime.End)
                {
                    await ReplotAsync(true, false);
                }
                else
                {
                    var displayRangeAndLoadedRangeRatio = (_AbscissaToDateTime(_LatestPlotRect.XMax) - _AbscissaToDateTime(_LatestPlotRect.XMin)).TotalMilliseconds / (_LoadedRangeDateTime.End - _LoadedRangeDateTime.Start).TotalMilliseconds;
                    if (displayRangeAndLoadedRangeRatio <= _MinDisplayRangeAndLoadedRangeRatio)
                    {
                        await ReplotAsync(true, false);
                    }
                }
            }

        }

        void replotFunc(bool plotMarkers, DataRect? plotRect, double[] axis_x, double[] axis_y, double[] axis_z, double[] axis_datetime)
        {
            var plotCount = axis_datetime.Length;
            if (plotCount == 0)
            {
                return;
            }
            XLineGraph.Plot(axis_datetime, axis_x);
            YLineGraph.Plot(axis_datetime, axis_y);
            ZLineGraph.Plot(axis_datetime, axis_z);

            if (plotMarkers && plotCount <= _MaxMarkerCountInLoadedRangePerAxis)
            {
                XMarkerGraph.PlotXY(axis_datetime, axis_x);
                YMarkerGraph.PlotXY(axis_datetime, axis_y);
                ZMarkerGraph.PlotXY(axis_datetime, axis_z);
            }
            else
            {
                XMarkerGraph.PlotXY(new double[0], new double[0]);
                YMarkerGraph.PlotXY(new double[0], new double[0]);
                ZMarkerGraph.PlotXY(new double[0], new double[0]);
            }
            if (plotRect != null)
            {
                XLineGraph.SetPlotRect((DataRect)plotRect);
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
        }


    }
}
