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
        }
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

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

            if (SettingViewModel.VR33BTerminal.ConnectionState == VR33BConnectionState.NotConnected || SettingViewModel.VR33BTerminal.ConnectionState == VR33BConnectionState.Failed)
            {
                try
                {
                    await SettingViewModel.VR33BTerminal.ConnectAsync();
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception);
                }
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
            var response = await SettingViewModel.VR33BTerminal.SetAccelerometerRangeAsync(targetAccRange);
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

        private async void CalibrateXButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await SettingViewModel.VR33BTerminal.CalibrateXAsync();
        }

        private async void CalibrateYButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingViewModel.VR33BTerminal.CalibrateYAsync();
        }

        private async void CalibrateZButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingViewModel.VR33BTerminal.CalibrateZAsync();
        }

        private async void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingViewModel.VR33BTerminal.ReadAllSettingAsync();
        }

        private async void SamplingThresholdSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            SamplingThresholdRing.Visibility = Visibility.Visible;
            var reponse = await SettingViewModel.VR33BTerminal.SetThresholdInPercent((int)SamplingThresholdSlider.Value);
            await Dispatcher.InvokeAsync(() => { SamplingThresholdRing.Visibility = Visibility.Collapsed; });
        }


        //时间按钮点击事件
        private async void CurrentTimeButton_Click(object sender, RoutedEventArgs e)
        {
            
            
            DataConfigurationWindow dataConfigurationWindow = new DataConfigurationWindow();
            var result = dataConfigurationWindow.ShowDialog();
            if(result.Value)
            {
                await SettingViewModel.VR33BTerminal.SetDateTimeAsync(dataConfigurationWindow.SelectedDateTime);
            }
            
        }
        private bool _AddressSetting = false;
        private async void AddressBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return && !_AddressSetting)
            {
                AddressBoxProgressRing.Visibility = Visibility.Visible;
                await SettingViewModel.VR33BTerminal.SetDeviceAddressAsync(byte.Parse(AddressBox.Text));
                await Dispatcher.InvokeAsync(() => { AddressBoxProgressRing.Visibility = Visibility.Collapsed; });
            }

        }

        private void AddressBox_LostFocus(object sender, RoutedEventArgs e)
        {
            AddressBox.InvalidateProperty(TextBox.TextProperty);

        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingViewModel.VR33BTerminal.ResetAllSetting();
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
                _VR33BTerminal.LatestSetting.OnThresholdInPercentChanged += LatestSetting_OnThresholdInPercentChanged;
                _VR33BTerminal.LatestSetting.OnThresholdChanged += LatestSetting_OnThresholdChanged;
                _VR33BTerminal.LatestSetting.OnAccelerometerSensibilityChanged += LatestSetting_OnAccelerometerSensibilityChanged;
                _VR33BTerminal.LatestSetting.OnAccelerometerZeroChanged += LatestSetting_OnAccelerometerZeroChanged;
                _VR33BTerminal.LatestSetting.OnSerialPortBaudRateChanged += LatestSetting_OnSerialPortBaudRateChanged;
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

        public int ThresholdInPercent
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 70;
                }
                return VR33BTerminal.LatestSetting.ThresholdInPercent;
            }
        }

        public double Threshold
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.Threshold;
            }
        }

        public VR33BSerialPortBaudRate BaudRate
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return VR33BSerialPortBaudRate._9600;
                }
                return VR33BTerminal.LatestSetting.SerialPortBaudRate;
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

        public UInt16 AccelerometerSensibilityX
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerSensibility.X;
            }
        }

        public UInt16 AccelerometerSensibilityY
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerSensibility.Y;
            }
        }

        public UInt16 AccelerometerSensibilityZ
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerSensibility.Z;
            }
        }

        public UInt16 AccelerommeterZeroX
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerZero.X;
            }
        }

        public UInt16 AccelerommeterZeroY
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerZero.Y;
            }
        }

        public UInt16 AccelerommeterZeroZ
        {
            get
            {
                if (VR33BTerminal == null)
                {
                    return 0;
                }
                return VR33BTerminal.LatestSetting.AccelerometerZero.Z;
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

        private void LatestSetting_OnThresholdInPercentChanged(object sender, int e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ThresholdInPercent"));
        }

        private void LatestSetting_OnThresholdChanged(object sender, double e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Threshold"));
        }
        private void LatestSetting_OnAccelerometerSensibilityChanged(object sender, (ushort X, ushort Y, ushort Z) e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerometerSensibilityX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerometerSensibilityY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerometerSensibilityZ"));
        }
        private void LatestSetting_OnAccelerometerZeroChanged(object sender, (ushort X, ushort Y, ushort Z) e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerommeterZeroX"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerommeterZeroY"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("AccelerommeterZeroZ"));
        }

        private void LatestSetting_OnSerialPortBaudRateChanged(object sender, VR33BSerialPortBaudRate e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BaudRate"));
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

    internal class ConnectionStateAndSamplingToEnableConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var connectionState = (VR33BConnectionState)values[0];
            var sampling = (bool)values[1];
            if(connectionState == VR33BConnectionState.Success && !sampling)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
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
