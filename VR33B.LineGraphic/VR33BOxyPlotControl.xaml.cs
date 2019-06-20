﻿using OxyPlot;
using OxyPlot.Axes;
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
    /// VR33BOxyPlotControl.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BOxyPlotControl : UserControl
    {
        public PlotModel OxyPlotModel { get; }
        public LineSeries XLineSeries { get; }
        public LineSeries YLineSeries { get; }
        public LineSeries ZLineSeries { get; }
        public TimeSpanAxis TimeSpanPlotAxis { get; }

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
                    _VR33BTerminal.VR33BSampleDataStorage.Updated -= VR33BSampleDataStorage_Updated;
                    //_VR33BTerminal.OnVR33BSampleValueReceived -= VR33BSampleDataStorage_Updated;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.VR33BSampleDataStorage.Updated += VR33BSampleDataStorage_Updated;
                //_VR33BTerminal.OnVR33BSampleValueReceived += VR33BSampleDataStorage_Updated;
            }
        }

        public bool TrackingModeOn { get; set; }

        private VR33BSampleValue[] _LoadedSampleValues;

        /// <summary>
        /// 关于这个看笔记
        /// 由于太麻烦了这个现在还没弄
        /// 现在的效果就是这个值等于0
        /// </summary>
        private double _ReloadRangeAndLoadedRangeRatio = 0.1;
        /// <summary>
        /// 看笔记
        /// </summary>
        private double _LoadedRangeAndDisplayRangeRatio = 3;
        /// <summary>
        /// 看笔记
        /// </summary>
        private double _MinDisplayRangeAndLoadedRangeRatio = 0.1;

        /// <summary>
        /// 这个数值表示在TrackingMode时在载入区域中最多的采样点
        /// 处于TrackingMode时数据不断加载，因此减少采样量以达到流畅
        /// </summary>
        private int _MaxLoadedSampleCountInTracking = 1000;

        private (TimeSpan Left, TimeSpan Right) _LoadedRangeTimeSpan;

        public TimeSpan _UpdateInterval
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 0);
            }
        }

        private DateTime _LastPlotDateTime = DateTime.Now;

        private DateTime _FirstSampleDateTime;

        private (double ActualMinimum, double ActualMaximum) _LatestPlotAxisActualMinMax;
        bool _TrackingModeReploting = false;
        private async void VR33BSampleDataStorage_Updated(object sender, VR33BSampleValue e)
        {
            if (e.SampleIndex == 0)
            {
                Inited = true;
                _FirstSampleDateTime = e.SampleDateTime;
            }
            if (DateTime.Now - _LastPlotDateTime >= _UpdateInterval && !_TrackingModeReploting)
            {
                _LastPlotDateTime = DateTime.Now;
                _TrackingModeReploting = true;
                await _ReplotAsync();
                _TrackingModeReploting = false;
            }
        }
        private Guid _LatestReplotGuid;

        /// <summary>
        /// 
        /// </summary>
        
        private Task _ReplotAsync()
        {
            var replotGuid = _LatestReplotGuid = Guid.NewGuid();
            double displayRange = _LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum;
            double newHalfLoadedRange = _LoadedRangeAndDisplayRangeRatio / 2 * displayRange;
            double displayMid = (_LatestPlotAxisActualMinMax.ActualMaximum + _LatestPlotAxisActualMinMax.ActualMinimum) / 2;
            double loadedLeft = displayMid - newHalfLoadedRange;
            double loadedRight = displayMid + newHalfLoadedRange;
            _LoadedRangeTimeSpan = (TimeSpanAxis.ToTimeSpan(loadedLeft), TimeSpanAxis.ToTimeSpan(loadedRight));
            return Task.Run(async () =>
            {
                var plotData = await VR33BTerminal.VR33BSampleDataStorage.GetFromDateTimeRange(_FirstSampleDateTime.Add( TimeSpanAxis.ToTimeSpan(loadedLeft)), _FirstSampleDateTime.Add(TimeSpanAxis.ToTimeSpan(loadedRight)));
                
                var plotCount = plotData.Count;
                if(plotCount == 0)
                {
                    return;
                }
                
                int beforeDownsample = plotData.Count;
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
                System.Diagnostics.Debug.WriteLine(plotCount);
                int downSample = plotCount;
                var xDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.X)).ToList();
                var yDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.Y)).ToList();
                var zDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.Z)).ToList();
                if(_LatestReplotGuid == replotGuid)
                {
                    XLineSeries.ItemsSource = xDataPoint;
                    YLineSeries.ItemsSource = yDataPoint;
                    ZLineSeries.ItemsSource = zDataPoint;
                    
                    if (TrackingModeOn)
                    {
                        double latestProgress = 0.5;
                        if (_LoadedSampleValues!=null && _LoadedSampleValues.Length > 0)
                        {
                            //latestProgress = TimeSpanAxis.ToDouble((_LoadedSampleValues.Last().SampleDateTime - _FirstSampleDateTime)) / (_LatestPlotAxisActualMinMax.ActualMinimum - _LatestPlotAxisActualMinMax.ActualMaximum);
                        }
                        
                        TimeSpanPlotAxis.Pan((((_LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum) * latestProgress + _LatestPlotAxisActualMinMax.ActualMinimum) - TimeSpanAxis.ToDouble(plotData.Last().SampleDateTime - _FirstSampleDateTime)) * TimeSpanPlotAxis.Scale);
                        
                    }
                    _LoadedSampleValues = plotData.ToArray();
                    OxyPlotView.InvalidatePlot();
                }
            });
        }

        public bool Inited { get; private set; }

        public VR33BOxyPlotControl()
        {
            InitializeComponent();
            DataContext = this;
            OxyPlotModel = new PlotModel();
            OxyPlotModel.Title = "Data";
            OxyPlotView.Model = OxyPlotModel;

            XLineSeries = new LineSeries();
            XLineSeries.Color = OxyColors.Red;
            //XLineSeries.MarkerType = MarkerType.Circle;
            //XLineSeries.MarkerFill = XLineSeries.Color;


            YLineSeries = new LineSeries();
            YLineSeries.Color = OxyColors.Green;
            //YLineSeries.MarkerType = MarkerType.Circle;
            //YLineSeries.MarkerFill = YLineSeries.Color;

            ZLineSeries = new LineSeries();
            ZLineSeries.Color = OxyColors.Blue;
            //ZLineSeries.MarkerType = MarkerType.Circle;
            //ZLineSeries.MarkerFill = ZLineSeries.Color;


            OxyPlotModel.Series.Add(XLineSeries);
            OxyPlotModel.Series.Add(YLineSeries);
            OxyPlotModel.Series.Add(ZLineSeries);

            TimeSpanPlotAxis = new TimeSpanAxis { Position = AxisPosition.Bottom, Minimum = TimeSpanAxis.ToDouble(new TimeSpan(0, 0, -1)), Maximum = TimeSpanAxis.ToDouble(new TimeSpan(0, 0, 1)) };
            OxyPlotModel.Axes.Add(TimeSpanPlotAxis);

            var AmpPlotAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = -1, Maximum = 1 };
            OxyPlotModel.Axes.Add(AmpPlotAxis);
            OxyPlotModel.Updating += OxyPlotModel_Updating;
            OxyPlotModel.Updated += OxyPlotModel_Updated;

            TrackingModeOn = true;
        }
        bool _OxyPlotUpdating = false;
        private void OxyPlotModel_Updated(object sender, EventArgs e)
        {
            _OxyPlotUpdating = false;
            //System.Diagnostics.Debug.WriteLine("Updated"+DateTime.Now);
        }

        private async void OxyPlotModel_Updating(object sender, EventArgs e)
        {
            _OxyPlotUpdating = true;
            _LatestPlotAxisActualMinMax = (TimeSpanPlotAxis.ActualMinimum, TimeSpanPlotAxis.ActualMaximum);
            if (!Inited)
            {
                return;
            }
            
            if (TrackingModeOn && VR33BTerminal.Sampling)
            {
                return;
            }
            if(TimeSpanAxis.ToTimeSpan(TimeSpanPlotAxis.ActualMinimum) < _LoadedRangeTimeSpan.Left || TimeSpanAxis.ToTimeSpan(TimeSpanPlotAxis.ActualMaximum) > _LoadedRangeTimeSpan.Right)
            {
                
                await _ReplotAsync();
            }
            else
            {
                var displayRangeAndLoadedRangeRatio = (TimeSpanPlotAxis.ActualMaximum - TimeSpanPlotAxis.ActualMinimum) / TimeSpanAxis.ToDouble(_LoadedRangeTimeSpan.Right - _LoadedRangeTimeSpan.Left);
                if(displayRangeAndLoadedRangeRatio < _MinDisplayRangeAndLoadedRangeRatio)
                {
                    await _ReplotAsync();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //PlotAxis.Pan((TimeSpanAxis.ToDouble(TimeSpan.FromSeconds(1))* PlotAxis.Scale));
            //XLineSeries.Points.Add(new DataPoint(TimeSpanAxis.ToDouble(new TimeSpan(0, 0, 10)), 10));
            //XLineSeries.Points.Add(new DataPoint(TimeSpanAxis.ToDouble(new TimeSpan(0, 0, 20)), 10));


            //Make 0:0:5 the center
            TimeSpanPlotAxis.Pan(((TimeSpanPlotAxis.ActualMinimum + TimeSpanPlotAxis.ActualMaximum) / 2 - TimeSpanAxis.ToDouble(TimeSpan.FromSeconds(5))) * TimeSpanPlotAxis.Scale);

            OxyPlotView.InvalidatePlot();
        }
    }


}