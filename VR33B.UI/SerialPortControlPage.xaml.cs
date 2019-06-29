using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        public TimeSpan _UpdateInterval
        {
            get
            {
                return TimeSpan.FromMilliseconds(500);
            }
        }

        private DateTime _LatestUpdateReceiveBoxDateTime = DateTime.Now;
        public SerialPortControlPage()
        {
            InitializeComponent();

        }


        /// <summary>
        /// 窗口大小改变时要改变两个RichTextBlock的值
        /// </summary>
        /// <param name="sizeInfo"></param>
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            ReceivedRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - ReceivedRawDataTitleBlock.ActualHeight - 10;
            SentRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - SentRawDataTitleBlock.ActualHeight - 10;
        }
        
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
                
            }
        }

        public delegate void OnStateChangedEventHandler(string stateMessage);

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

        private void ViewModel_OnReceived(object sender, VR33BReceiveData e)
        {
            var hexStringArray = from receiveByte in e.RawByteArray
                                 select string.Format("{0:x2}", receiveByte);
            var hexString = string.Join(" ", hexStringArray);

            if (DateTime.Now - _LatestUpdateReceiveBoxDateTime > _UpdateInterval)
            {
                _LatestUpdateReceiveBoxDateTime = DateTime.Now;
                ViewModel.ReceiveBoxText += (" " + hexString);
            }

        }

        byte _HexStringToByte(string hexString)
        {
            byte hex = 0;
            if(hexString[0] >= '0' && hexString[0] <= '9')
            {
                hex = (byte)(hexString[0] - '0');
            }
            else
            {
                hex = (byte)(hexString[0] - 'a' + 10);
            }
            if(hexString.Length >= 2)
            {
                hex = (byte)(hex << 4);
                if (hexString[1] >= '0' && hexString[1] <= '9')
                {
                    hex |= (byte)(hexString[1] - '0');
                }
                else
                {
                    hex |= (byte)(hexString[1] - 'a' + 10);
                }
            }
            return hex;
        }

        private async void SendBox_KeyDown(object sender, KeyEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine(e.Key);
            if(e.Key != Key.Return)
            {
                return;
            }
            var text = (sender as TextBox).ToString();
            var hexStrs = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<byte> hexList = new List<byte>();
            foreach(var hexStr in hexStrs)
            {
                hexList.Add(_HexStringToByte(hexStr));
            }
            byte[] byteArray = hexList.ToArray();
            if(ViewModel.SerialPort.IsOpen)
            {
                await ViewModel.SerialPort.BaseStream.WriteAsync(byteArray, 0, byteArray.Length);
            }
            else
            {

            }
        }

        private void SendBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex match = new Regex(@"^([0-9a-hA-H]{1,2}\s+)*([0-9a-hA-H]{1,2}\s*)$");
            if(match.IsMatch((sender as TextBox).Text + e.Text))
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue)
            {
                ViewModel.AvaliablePortNames.Clear();
                foreach (var serialPortName in SerialPort.GetPortNames())
                {
                    ViewModel.AvaliablePortNames.Add(serialPortName);
                }
                ViewModel.OnReceived += ViewModel_OnReceived;
                SerialNoBox.SelectedValue = ViewModel.PortName;
            }
            else
            {
                ViewModel.OnReceived -= ViewModel_OnReceived;
                
            }

        }

        private UInt16 Crc16(byte[] buf)
        {
            UInt16 i, j, crc;
            crc = 0xffff;
            int length = buf.Length;
            for (i = 0; i < length; i++)
            {
                crc ^= (UInt16)buf[i]; //°´Î»È¡·´
                for (j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xa001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        private void SendBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var hexSplitMatch = new Regex(@"\b[0-9a-hA-H]{1,2}\b");
            var spliteResult = hexSplitMatch.Matches(SendBox.Text);
            var hexBytes = new List<byte>();
            foreach(var hexStr in spliteResult)
            {
                hexBytes.Add(_HexStringToByte(((Match)hexStr).Value));
            }
            var crc = Crc16(hexBytes.ToArray());
            var crcBytes = BitConverter.GetBytes(crc);
            StringBuilder sb = new StringBuilder();
            foreach(var crcByte in crcBytes)
            {
                sb.AppendFormat("{0:X2} ", crcByte);
            }
            CrcBox.Text = sb.ToString();
            

        }
    }

    public class SerialPortViewModel:INotifyPropertyChanged
    {

        private VR33BTerminal _VR33BTerminal;

        public event EventHandler<VR33BReceiveData> OnReceived;
        public event EventHandler<VR33BSendData> OnSerialPortSent;

        public ObservableCollection<string> AvaliablePortNames { get; }
        public ObservableCollection<int> AvaliableBaudRate { get; }
        public ObservableCollection<int> AvaliableDataBits { get; }
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("PortName"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BaudRate"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("DataBits"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("StopBits"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Parity"));
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

        private bool _CustomSerialPortOperation;
        public bool CustomSerialPortOperation
        {
            get
            {
                return _CustomSerialPortOperation;
            }
            set
            {
                _CustomSerialPortOperation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CustomSerialPortOperation"));
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
                if(value == null)
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
                    return 9600;
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

        public object StopBitsSource
        {
            get
            {
                return Enum.GetValues(typeof(StopBits));
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
        public object ParitySource
        {
            get
            {
                return Enum.GetValues(typeof(Parity));
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

        string _ReceiveBoxText;
        public string ReceiveBoxText
        {
            get
            {
                return _ReceiveBoxText;
            }
            set
            {
                _ReceiveBoxText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ReceiveBoxText"));
            }
        }

        string _SendBoxText;
        public string SendBoxText
        {
            get
            {
                return _SendBoxText;
            }
            set
            {
                _SendBoxText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SendBoxText"));
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
            AvaliablePortNames = new ObservableCollection<string>();
            AvaliableBaudRate = new ObservableCollection<int>() { 9600, 115200 };
            AvaliableDataBits = new ObservableCollection<int>() { 8, 7, 6 };
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
