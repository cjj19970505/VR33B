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
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace VR33B.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();


            serialPort = new SerialPort();
            serialPort.DataReceived += SerialPort_DataReceived;
        }

        //接收到数据
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            //ReceivedRawDataBox.AppendText(indata);
        }

        private ObservableCollection<string> serialPortNames;
        private ObservableCollection<int> baudRates = new ObservableCollection<int> { 9600 };
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

        SerialPort serialPort;
        private void SwitchPortButton_Click(object sender, RoutedEventArgs e)
        {
            //serialPort.PortName = (string)SerialNoBox.SelectedItem;
            //serialPort.BaudRate = (int)BaudRateBox.SelectedItem;
            //serialPort.DataBits = (int)DataBitBox.SelectedItem;
            //serialPort.Parity = (Parity)ParityBitBox.SelectedItem;
            //serialPort.StopBits = (StopBits)StopBitBox.SelectedItem;
            try
            {
                if (!serialPort.IsOpen)
                {
                    serialPort.Open();
                }
                else
                {
                    serialPort.Close();
                }
            }
            catch (Exception exception)
            {
                StateBox.Text = exception.Message;
            }
        }


    }
}
