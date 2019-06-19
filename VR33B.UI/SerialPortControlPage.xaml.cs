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

        private ObservableCollection<string> serialPortNames;
        private ObservableCollection<int> baudRates = new ObservableCollection<int> { 9600 };
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            ReceivedRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - ReceivedRawDataTitleBlock.ActualHeight - 10;
            SentRawDataBox.Height = RowDataGrid.ActualHeight / 2 - 20 - SentRawDataTitleBlock.ActualHeight - 10;
            System.Diagnostics.Debug.WriteLine(ReceivedRawDataBox.Height);
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void SwitchPortButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
