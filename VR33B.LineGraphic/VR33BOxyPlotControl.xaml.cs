using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Xml.Serialization;

namespace VR33B.LineGraphic
{
    /// <summary>
    /// VR33BOxyPlotControl.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BOxyPlotControl : UserControl, INotifyPropertyChanged
    {
        public const string SettingFileName = "VR33BOxyPlotSetting.xml";
        public event PropertyChangedEventHandler PropertyChanged;
        public PlotModel OxyPlotModel { get; }
        public LineSeries XLineSeries { get; }
        public LineSeries YLineSeries { get; }
        public LineSeries ZLineSeries { get; }

        public LineSeries IndicatorSeries { get; }
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
                    _VR33BTerminal.OnVR33BSampleStarted -= _VR33BTerminal_OnVR33BSampleStarted;
                    _VR33BTerminal.OnVR33BSampleEnded -= _VR33BTerminal_OnVR33BSampleEnded;
                    //_VR33BTerminal.OnVR33BSampleValueReceived -= VR33BSampleDataStorage_Updated;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.VR33BSampleDataStorage.Updated += VR33BSampleDataStorage_Updated;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
                //_VR33BTerminal.OnVR33BSampleValueReceived += VR33BSampleDataStorage_Updated;
            }
        }

        public VR33BOxyPlotSetting Setting { get; }

        private void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, VR33BSampleProcess e)
        {
            OxyPlotModel.Title = e.Guid.ToString();
            _LoadedSampleValues = new VR33BSampleValue[0];
            TimeSpanPlotAxis.Pan((((_LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum) * 0.5 + _LatestPlotAxisActualMinMax.ActualMinimum) - 0) * TimeSpanPlotAxis.Scale);
            OxyPlotView.InvalidatePlot();

        }

        bool _TrackingModeOn;
        public bool TrackingModeOn
        {
            get
            {
                return _TrackingModeOn;
            }
            set
            {
                _TrackingModeOn = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TrackingModeOn"));
            }
        }

        private VR33BSampleValue[] _LoadedSampleValues;

        /// <summary>
        /// 关于这个看笔记
        /// 由于太麻烦了这个现在还没弄
        /// 现在的效果就是这个值等于0
        /// </summary>
        //private double _ReloadRangeAndLoadedRangeRatio = 0.1;
        /// <summary>
        /// 看笔记
        /// </summary>
        //private double _LoadedRangeAndDisplayRangeRatio = 3;
        /// <summary>
        /// 看笔记
        /// </summary>
        //private double _MinDisplayRangeAndLoadedRangeRatio = 0.1;

        /// <summary>
        /// 这个数值表示在TrackingMode时在载入区域中最多的采样点
        /// 处于TrackingMode时数据不断加载，因此减少采样量以达到流畅
        /// </summary>
        //private int _MaxLoadedSampleCountInTracking = 1000;

        private (TimeSpan Left, TimeSpan Right) _LoadedRangeTimeSpan;

        private bool _Visible = false;

        public TimeSpan _UpdateInterval
        {
            get
            {
                var possibleInterval = Setting.BaseUpdateTimeSpan + TimeSpan.FromMilliseconds(_LatestPlotTimeSpan.TotalMilliseconds * 150);
                if (possibleInterval < Setting.MaxUpdateTimeSpan)
                {
                    return possibleInterval;
                }
                else
                {
                    return TimeSpan.FromMilliseconds(500);
                }

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
            if (!_Visible)
            {
                return;
            }
            if (DateTime.Now - _LastPlotDateTime >= _UpdateInterval && !_TrackingModeReploting)
            {
                _LastPlotDateTime = DateTime.Now;
                if (!TrackingModeOn && e.SampleDateTime > _FirstSampleDateTime.Add(_LoadedRangeTimeSpan.Right))
                {

                }
                else
                {
                    _TrackingModeReploting = true;
                    await _ReplotAsync();
                    _TrackingModeReploting = false;
                }

            }
        }
        private Guid _LatestReplotGuid;
        private Task _ReplotAsync()
        {
            var replotGuid = _LatestReplotGuid = Guid.NewGuid();
            double displayRange = _LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum;
            //double newHalfLoadedRange = _LoadedRangeAndDisplayRangeRatio / 2 * displayRange;
            double newHalfLoadedRange = Setting.LoadedRangeAndDisplayRangeRatio / 2 * displayRange;
            double displayMid = (_LatestPlotAxisActualMinMax.ActualMaximum + _LatestPlotAxisActualMinMax.ActualMinimum) / 2;
            double loadedLeft = displayMid - newHalfLoadedRange;
            double loadedRight = displayMid + newHalfLoadedRange;
            _LoadedRangeTimeSpan = (TimeSpanAxis.ToTimeSpan(loadedLeft), TimeSpanAxis.ToTimeSpan(loadedRight));
            return Task.Run(async () =>
            {
                var plotData = await VR33BTerminal.VR33BSampleDataStorage.GetFromDateTimeRangeAsync(_FirstSampleDateTime.Add(TimeSpanAxis.ToTimeSpan(loadedLeft)), _FirstSampleDateTime.Add(TimeSpanAxis.ToTimeSpan(loadedRight)));

                var plotCount = plotData.Count;
                if (plotCount == 0)
                {
                    return;
                }

                int beforeDownsample = plotData.Count;
                if (plotCount >= 2 * Setting.MaxLoadedSampleCountInTracking)
                {
                    int step = plotCount / Setting.MaxLoadedSampleCountInTracking;
                    plotData = (from sampleValue in plotData
                                where sampleValue.SampleIndex % step == 0
                                select sampleValue).ToList();
                    plotCount = plotData.Count;
                }
                int downSample = plotCount;
                var xDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.X)).ToList();
                var yDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.Y)).ToList();
                var zDataPoint = (from sampleValue in plotData
                                  select new DataPoint(TimeSpanAxis.ToDouble(sampleValue.SampleDateTime - _FirstSampleDateTime), sampleValue.AccelerometerValue.Z)).ToList();
                if (_LatestReplotGuid == replotGuid)
                {
                    XLineSeries.ItemsSource = xDataPoint;
                    YLineSeries.ItemsSource = yDataPoint;
                    ZLineSeries.ItemsSource = zDataPoint;

                    if (TrackingModeOn)
                    {
                        double latestProgress = 0.5;
                        if (_LoadedSampleValues != null && _LoadedSampleValues.Length > 0)
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
            var defaultSetting = new VR33BOxyPlotSetting()
            {
                ReloadRangeAndLoadedRangeRatio = 0.1,
                LoadedRangeAndDisplayRangeRatio = 3,
                MinDisplayRangeAndLoadedRangeRatio = 0.1,
                MaxLoadedSampleCountInTracking = 1000,
                BaseUpdateTimeSpan = TimeSpan.FromMilliseconds(15),
                MaxUpdateTimeSpan = TimeSpan.FromMilliseconds(500)
            };
            var fileName = SettingFileName;
            var filePath = Environment.CurrentDirectory + "//" + fileName;
            XmlSerializer serializer = new XmlSerializer(typeof(VR33BOxyPlotSetting));
            if(!File.Exists(filePath))
            {
                using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    serializer.Serialize(settingStream, defaultSetting);
                };
                Setting = defaultSetting;
            }
            else
            {
                using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    Setting = (VR33BOxyPlotSetting)serializer.Deserialize(settingStream);
                };
            }

            DataContext = this;
            _Visible = Visibility == Visibility.Visible;
            OxyPlotModel = new PlotModel();
            OxyPlotModel.Title = "Data";
            OxyPlotView.Model = OxyPlotModel;

            XLineSeries = new LineSeries();
            XLineSeries.Color = OxyColors.Red;
            XLineSeries.MarkerType = MarkerType.Circle;
            XLineSeries.MarkerFill = XLineSeries.Color;
            XLineSeries.Title = "X-Axis";

            YLineSeries = new LineSeries();
            YLineSeries.Color = OxyColors.Green;
            YLineSeries.MarkerType = MarkerType.Circle;
            YLineSeries.MarkerFill = YLineSeries.Color;
            YLineSeries.Title = "Y-Axis";

            ZLineSeries = new LineSeries();
            ZLineSeries.Color = OxyColors.Blue;
            ZLineSeries.MarkerType = MarkerType.Circle;
            ZLineSeries.MarkerFill = ZLineSeries.Color;
            ZLineSeries.Title = "Y-Axis";

            IndicatorSeries = new LineSeries();
            IndicatorSeries.Color = OxyColors.Black;
            IndicatorSeries.Title = "Indicator";
            IndicatorSeries.StrokeThickness = 1;

            OxyPlotModel.Series.Add(XLineSeries);
            OxyPlotModel.Series.Add(YLineSeries);
            OxyPlotModel.Series.Add(ZLineSeries);
            OxyPlotModel.Series.Add(IndicatorSeries);

            XAxisLegendView.DataContext = XLineSeries;
            YAxisLegendView.DataContext = YLineSeries;
            ZAxisLegendView.DataContext = ZLineSeries;

            TimeSpanPlotAxis = new TimeSpanAxis { Position = AxisPosition.Bottom, Minimum = TimeSpanAxis.ToDouble(new TimeSpan(0, 0, -1)), Maximum = TimeSpanAxis.ToDouble(new TimeSpan(0, 0, 1)) };
            OxyPlotModel.Axes.Add(TimeSpanPlotAxis);

            var AmpPlotAxis = new LinearAxis { Position = AxisPosition.Left, Minimum = -1, Maximum = 1 };
            OxyPlotModel.Axes.Add(AmpPlotAxis);
            OxyPlotModel.Updating += OxyPlotModel_Updating;
            OxyPlotModel.Updated += OxyPlotModel_Updated;

            IsVisibleChanged += VR33BOxyPlotControl_IsVisibleChanged;
            TrackingModeOn = true;
        }

        private void VR33BOxyPlotControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _Visible = (bool)e.NewValue;
            if (!_Visible)
            {
                OxyPlotView.IsEnabled = false;
            }
            else
            {
                OxyPlotView.IsEnabled = true;
            }
        }

        DateTime _LatestBeginPlotDateTime = DateTime.Now;
        TimeSpan _LatestPlotTimeSpan = new TimeSpan(0);

        private void OxyPlotModel_Updated(object sender, EventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("PlotTimeSpan:"+(DateTime.Now - _LatestBeginPlotDateTime).TotalMilliseconds);
            _LatestPlotTimeSpan = DateTime.Now - _LatestBeginPlotDateTime;
            //System.Diagnostics.Debug.WriteLine("Updated"+DateTime.Now);
        }

        private async void OxyPlotModel_Updating(object sender, EventArgs e)
        {
            _LatestBeginPlotDateTime = DateTime.Now;
            _LatestPlotAxisActualMinMax = (TimeSpanPlotAxis.ActualMinimum, TimeSpanPlotAxis.ActualMaximum);
            if (!Inited)
            {
                return;
            }

            if (TrackingModeOn && VR33BTerminal.Sampling)
            {
                return;
            }
            if (TimeSpanAxis.ToTimeSpan(TimeSpanPlotAxis.ActualMinimum) < _LoadedRangeTimeSpan.Left || TimeSpanAxis.ToTimeSpan(TimeSpanPlotAxis.ActualMaximum) > _LoadedRangeTimeSpan.Right)
            {

                await _ReplotAsync();
            }
            else
            {
                var displayRangeAndLoadedRangeRatio = (TimeSpanPlotAxis.ActualMaximum - TimeSpanPlotAxis.ActualMinimum) / TimeSpanAxis.ToDouble(_LoadedRangeTimeSpan.Right - _LoadedRangeTimeSpan.Left);
                if (displayRangeAndLoadedRangeRatio < Setting.MinDisplayRangeAndLoadedRangeRatio)
                {
                    await _ReplotAsync();
                }
            }
        }

        public void PanTo(DateTime dateTime)
        {
            TimeSpanPlotAxis.Pan((((_LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum) * 0.5 + _LatestPlotAxisActualMinMax.ActualMinimum) - TimeSpanAxis.ToDouble(dateTime - _FirstSampleDateTime)) * TimeSpanPlotAxis.Scale);
            OxyPlotView.InvalidatePlot(false);
        }

        public void Indicate(DateTime dateTime)
        {
            double x = TimeSpanAxis.ToDouble((dateTime - _FirstSampleDateTime));
            IndicatorSeries.ItemsSource = new DataPoint[2] { new DataPoint(x, 10), new DataPoint(x, -10) };
            TimeSpanPlotAxis.Pan((((_LatestPlotAxisActualMinMax.ActualMaximum - _LatestPlotAxisActualMinMax.ActualMinimum) * 0.5 + _LatestPlotAxisActualMinMax.ActualMinimum) - TimeSpanAxis.ToDouble(dateTime - _FirstSampleDateTime)) * TimeSpanPlotAxis.Scale);
            OxyPlotView.InvalidatePlot(true);
        }

    }

    public struct VR33BOxyPlotSetting
    {
        public double ReloadRangeAndLoadedRangeRatio { get; set; }
        public double LoadedRangeAndDisplayRangeRatio { get; set; }
        public double MinDisplayRangeAndLoadedRangeRatio { get; set; }
        public int MaxLoadedSampleCountInTracking { get; set; }
        [XmlIgnore]
        public TimeSpan BaseUpdateTimeSpan { get; set; }
        [XmlIgnore]
        public TimeSpan MaxUpdateTimeSpan { get; set; }

        public double BaseUpdateTimeSpanInMS
        {
            get
            {
                return BaseUpdateTimeSpan.TotalMilliseconds;
            }
            set
            {
                BaseUpdateTimeSpan = TimeSpan.FromMilliseconds(value);
            }
        }

        public double MaxUpdateTimeSpanInMs
        {
            get
            {
                return MaxUpdateTimeSpan.TotalMilliseconds;
            }
            set
            {
                MaxUpdateTimeSpan = TimeSpan.FromMilliseconds(value);
            }
        }
    }


}
