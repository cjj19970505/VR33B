using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            DataBitBox.SelectedItem = dataBits[0];
            StopBitBox.ItemsSource = Enum.GetValues(typeof(StopBits));
            StopBitBox.SelectedItem = StopBits.One;
            ParityBitBox.ItemsSource = Enum.GetValues(typeof(Parity));
            ParityBitBox.SelectedItem = Parity.None;
        }
        public static SerialPort SensorSerialPort = new SerialPort();
        private ObservableCollection<string> serialPortNames;
        private ObservableCollection<int> baudRates = new ObservableCollection<int> { 9600 };
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
            SensorSerialPort.PortName = (string)SerialNoBox.SelectedItem;
            SensorSerialPort.BaudRate = (int)BaudRateBox.SelectedItem;
            SensorSerialPort.DataBits = (int)DataBitBox.SelectedItem;
            SensorSerialPort.Parity = (Parity)ParityBitBox.SelectedItem;
            SensorSerialPort.StopBits = (StopBits)StopBitBox.SelectedItem;
            try
            {
                if (!SensorSerialPort.IsOpen)
                {
                    SensorSerialPort.Open();
                }
                else
                {
                    SensorSerialPort.Close();
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
    }
}
