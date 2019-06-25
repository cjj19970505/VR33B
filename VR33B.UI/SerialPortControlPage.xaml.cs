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
    /// SerialPortControlPage.xaml 的交互逻辑
    /// </summary>
    public partial class SerialPortControlPage : Page
    {
        public SerialPort SerialPort
        {
            get
            {
                return ViewModel.SerialPort;
            }
        }
        public SerialPortControlPage()
        {
            InitializeComponent();
            serialPortNames = new ObservableCollection<string>(SerialPort.GetPortNames());
            if (serialPortNames.Count == 0)
            {
                serialPortNames.Add("无串口");
            }
            SerialNoBox.ItemsSource = serialPortNames;
            SerialNoBox.SelectedItem = serialPortNames[0];
            BaudRateBox.ItemsSource = baudRates;
            BaudRateBox.SelectedItem = baudRates[0];
            DataBitBox.ItemsSource = dataBits;
            //DataBitBox.SelectedItem = dataBits[0];
            DataBitBox.SelectedValue = dataBits[0];
            StopBitBox.ItemsSource = Enum.GetValues(typeof(StopBits));
            //StopBitBox.SelectedItem = StopBits.One;
            ParityBitBox.ItemsSource = Enum.GetValues(typeof(Parity));


        }

        private ObservableCollection<string> serialPortNames;
        private ObservableCollection<int> baudRates = new ObservableCollection<int> { 115200 };
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

        /// <summary>
        /// 窗口大小改变时要改变两个RichTextBlock的值
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            ReceivedRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - ReceivedRawDataTitleBlock.ActualHeight - 10;
            SentRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - SentRawDataTitleBlock.ActualHeight - 10;
        }
        
        /// <summary>
        /// 打开串口按钮点击
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SwitchPortButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!SerialPort.IsOpen)
                {
                    ViewModel.Open();
                }
                else
                {
                    ViewModel.Close();
                }
            }
            catch (Exception exception)
            {
                OnStateChanged(exception.Message);
            }
        }

        public delegate void OnStateChangedEventHandler(string stateMessage);
        
        /// <summary>
        /// 当然在该页面有状态改变时触发该事件
        /// </summary>
        static public event OnStateChangedEventHandler OnStateChanged;

        
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            serialPortNames.Clear();
            foreach (var serialPortName in SerialPort.GetPortNames())
            {
                serialPortNames.Add(serialPortName);
                SerialNoBox.SelectedItem = serialPortNames[0];
            }
            //ViewModel.OnReceived += ViewModel_OnReceived;
            //ViewModel.OnSerialPortSent += ViewModel_OnSerialPortSent;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            //ViewModel.OnReceived -= ViewModel_OnReceived;
            //ViewModel.OnSerialPortSent -= ViewModel_OnSerialPortSent;
        }

        private async void ViewModel_OnSerialPortSent(object sender, VR33BSendData e)
        {
            var hexStringArray = from receiveByte in e.SendBytes
                                 select string.Format("{0:x2}", receiveByte);
            var hexString = string.Join(" ", hexStringArray) + Environment.NewLine;
            await Dispatcher.InvokeAsync(() =>
            {
                SentRawDataBox.Text += hexString;
            });
        }

        private async void ViewModel_OnReceived(object sender, VR33BReceiveData e)
        {
            var hexStringArray = from receiveByte in e.RawByteArray
                                 select string.Format("{0:x2}", receiveByte);
            var hexString = string.Join(" ", hexStringArray);
            await Dispatcher.InvokeAsync(() =>
            {
                ReceivedRawDataBox.Text += hexString;
            });
        }

        private void SendBox_KeyDown(object sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.Key);
        }
    }

    public class SerialPortViewModel:INotifyPropertyChanged
    {

        private VR33BTerminal _VR33BTerminal;

        public event EventHandler<VR33BReceiveData> OnReceived;
        public event EventHandler<VR33BSendData> OnSerialPortSent;

        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            set
            {
                if(_VR33BTerminal != null)
                {

                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnReceived += OnReceived;
                _VR33BTerminal.OnSerialPortSent += OnSerialPortSent;

            }
        }
        


        public SerialPort SerialPort
        {
            get
            {
                if(VR33BTerminal == null)
                {
                    return null;
                }
                return VR33BTerminal.SerialPort;
            }
        }
        public string PortName
        {
            get
            {
                if(SerialPort == null)
                {
                    return "";
                }
                return SerialPort.PortName;
            }
            set
            {
                if(SerialPort == null)
                {
                    return;
                }
                SerialPort.PortName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortName"));
            }
        }
        public int BaudRate
        {
            get
            {
                if(SerialPort == null)
                {
                    return 0;
                }
                return SerialPort.BaudRate;
            }
            set
            {
                if(SerialPort == null)
                {
                    return;
                }
                SerialPort.BaudRate = SerialPort.BaudRate;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BaudRate"));
            }
        }
        public int DataBits
        {
            get
            {
                if(SerialPort == null)
                {
                    return 0;
                }
                return SerialPort.DataBits;
            }
            set
            {
                if(SerialPort == null)
                {
                    return;
                }
                SerialPort.DataBits = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DataBits"));
            }
        }
        public StopBits StopBits
        {
            get
            {
                if (SerialPort == null)
                {
                    return StopBits.None;
                }
                return SerialPort.StopBits;
            }
            set
            {
                if(SerialPort == null)
                {
                    return;
                }

                SerialPort.StopBits = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StopBits"));
            }
        }
        public Parity Parity
        {
            get
            {
                if(SerialPort == null)
                {
                    return Parity.None;
                }
                return SerialPort.Parity;
            }
            set
            {
                if(SerialPort == null)
                {
                    return;
                }
                SerialPort.Parity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Parity"));
            }
        }

        public bool IsOpen
        {
            get
            {
                if(SerialPort == null)
                {
                    return false;
                }
                return SerialPort.IsOpen;
            }
        }

        public void Open()
        {
            SerialPort.Open();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsOpen"));
        }
        public void Close()
        {
            SerialPort.Close();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsOpen"));
        }

        public SerialPortViewModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class SerialIsOpenToButtonContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isOpen = (bool)value;
            if(isOpen)
            {
                return "关闭串口";
            }
            else
            {
                return "打开串口";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
