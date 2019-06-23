using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
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

namespace VR33B.UI
{
    /// <summary>
    /// SensorConfigurePage.xaml 的交互逻辑
    /// </summary>
    public partial class SensorConfigurePage : Page
    {
        public SensorConfigurePage()
        {
            InitializeComponent();
            SensorStopBitBox.ItemsSource = Enum.GetValues(typeof(StopBits));
            SensorStopBitBox.SelectedItem = StopBits.One;
            SensorParityBox.ItemsSource = Enum.GetValues(typeof(Parity));
            SensorParityBox.SelectedItem = Parity.None;
        }
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

        private void SamplingThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SamplingThresholdValueBlock.Text = ((int)e.NewValue).ToString() + "%";
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            SamplingThresholdSlider.Width = SamplingThresholdColumn.ActualWidth - SamplingThresholdValueBlock.ActualWidth - 15;
        }

        private async void SamplingRateBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count == 0 || SamplingRateBox.SelectedValue == null || !SamplingRateBox.IsDropDownOpen)
            {
                return;
            }
            VR33BSampleFrequence targetSampleFrequncy = (VR33BSampleFrequence)SamplingRateBox.SelectedValue;
            SamplingRateRing.Visibility = Visibility.Visible;
            var response = await SettingViewModel.VR33BTerminal.SetSampleFrequencyAsync(targetSampleFrequncy);
            await Dispatcher.InvokeAsync(() => { SamplingRateRing.Visibility = Visibility.Collapsed; });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if(SettingViewModel.VR33BTerminal.ConnectionState == VR33BConnectionState.NotConnected || SettingViewModel.VR33BTerminal.ConnectionState == VR33BConnectionState.Failed)
            {
                await SettingViewModel.VR33BTerminal.ConnectAsync();
            }
            
        }

        private async void AccelerometerRangeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0 || !AccelerometerRangeBox.IsDropDownOpen)
            {
                return;
            }
            VR33BAccelerometerRange targetAccRange = (VR33BAccelerometerRange)AccelerometerRangeBox.SelectedValue;
            AccelerometerRangeProgressRing.Visibility = Visibility.Visible;
            var response = await SettingViewModel.VR33BTerminal.SetAccelerometerRange(targetAccRange);
            await Dispatcher.InvokeAsync(() => { AccelerometerRangeProgressRing.Visibility = Visibility.Collapsed; });
        }

        private async void SampleButton_Click(object sender, RoutedEventArgs e)
        {
            if(!SettingViewModel.VR33BTerminal.Sampling)
            {
                await SettingViewModel.VR33BTerminal.StartSampleAsync();
            }
            else
            {
                await SettingViewModel.VR33BTerminal.StopSampleAsync();
            }
            
        }
    }

    public class VR33BSettingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
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

                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnConnectonStateChanged += _VR33BTerminal_OnConnectonStateChanged;
                _VR33BTerminal.LatestSetting.OnDeviceAddressChanged += LatestSetting_OnDeviceAddressChanged;
                _VR33BTerminal.LatestSetting.OnSampleFrequencyChanged += LatestSetting_OnSampleFrequencyChanged;
                _VR33BTerminal.LatestSetting.OnAccelerometerRangeChanged += LatestSetting_OnAccelerometerRangeChanged;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
            }
        }

        

        public object SampleFrequencysSource
        {
            get
            {
                return Enum.GetValues(typeof(VR33BSampleFrequence)).Cast<VR33BSampleFrequence>().Select((value) =>
                {
                    var description = Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return new { Description = description.Description, Value = value };
                }).ToList();
            }
        }

        public object AccelerometerRangeSource
        {
            get
            {
                return Enum.GetValues(typeof(VR33BAccelerometerRange)).Cast<VR33BAccelerometerRange>().Select((value) =>
                {
                    var description = Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return new { Description = description.Description, Value = value };
                }).ToList();
            }
        }

        public object BaudRateSource
        {
            get
            {
                return Enum.GetValues(typeof(VR33BSerialPortBaudRate)).Cast<VR33BSerialPortBaudRate>().Select((value) =>
                {
                    var description = Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) as DescriptionAttribute;
                    return new { Description = description.Description, Value = value };
                }).ToList();
            }
        }
        

        public byte DeviceAddress
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.DeviceAddress;
            }
        }

        public VR33BSampleFrequence SampleFrequency
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return VR33BSampleFrequence._1Hz;
                }
                return VR33BTerminal.LatestSetting.SampleFrequence;
            }
        }

        public VR33BAccelerometerRange AccelerometerRange
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return VR33BAccelerometerRange._8g;
                }
                return VR33BTerminal.LatestSetting.AccelerometerRange;
            }
        }

        public bool Sampling
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return false;
                }
                return VR33BTerminal.Sampling;
            }
        }
        private void LatestSetting_OnDeviceAddressChanged(object sender, byte e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DeviceAddress"));
        }
        private void LatestSetting_OnSampleFrequencyChanged(object sender, VR33BSampleFrequence e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SampleFrequency"));
        }

        private void LatestSetting_OnAccelerometerRangeChanged(object sender, VR33BAccelerometerRange e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerometerRange"));
        }

        public VR33BConnectionState ConnectionState
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return VR33BConnectionState.NotConnected;
                }
                return VR33BTerminal.ConnectionState;
            }
        }

        private void _VR33BTerminal_OnConnectonStateChanged(object sender, VR33BConnectionState e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ConnectionState"));
        }
        private void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sampling"));
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, VR33BSampleProcess e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Sampling"));
        }

        
    }

    internal class ConnectionStateToButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            VR33BConnectionState connectionState = (VR33BConnectionState)value;
            switch(connectionState)
            {
                case VR33BConnectionState.NotConnected:
                    return "未连接";
                case VR33BConnectionState.Connecting:
                    return "连接中";
                case VR33BConnectionState.Success:
                    return "已连接";
                case VR33BConnectionState.Failed:
                    return "连接失败";
            }
            return "WHAT??";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class ConnectionStateToEnableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            VR33BConnectionState connectionState = (VR33BConnectionState)value;
            if(connectionState == VR33BConnectionState.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class DeviceAddressToTextBoxContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var deviceAddress = (byte)value;
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    internal class SamplingToButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool sampling = (bool)value;
            if(sampling)
            {
                return "采样中";
            }
            else
            {
                return "开始采样";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
