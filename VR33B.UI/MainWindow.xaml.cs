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
using VR33B.Storage;
using System.Xml.Serialization;
using System.IO;

namespace VR33B.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string PCSerialPortSettingName = "PCSerialPortSetting.xml";
        private static VR33BTerminal _VR33BTerminal;
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                if (_VR33BTerminal == null)
                {
                    _VR33BTerminal = new VR33BTerminal(new VR33BSqliteStorage(), false);
                    (_VR33BTerminal.VR33BSampleDataStorage as VR33BSqliteStorage).SampleTimeDispatcher = new VR33BSampleTimeDispatcher(_VR33BTerminal);

                    var fileName = PCSerialPortSettingName;
                    var filePath = Environment.CurrentDirectory + "//" + fileName;
                    XmlSerializer serializer = new XmlSerializer(typeof(PCSerialPortSetting));
                    if (!File.Exists(filePath))
                    {
                    }
                    else
                    {
                        using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            var serialPortSetting = (PCSerialPortSetting)serializer.Deserialize(settingStream);
                            _VR33BTerminal.SerialPort.PortName = serialPortSetting.PortName;
                            _VR33BTerminal.SerialPort.BaudRate = serialPortSetting.BaudRate;
                            _VR33BTerminal.SerialPort.StopBits = serialPortSetting.StopBits;
                            _VR33BTerminal.SerialPort.DataBits = serialPortSetting.DataBits;
                            _VR33BTerminal.SerialPort.Handshake = serialPortSetting.HandShake;
                            _VR33BTerminal.SerialPort.RtsEnable = serialPortSetting.RtsEnable;
                        };
                    }
                }
                return _VR33BTerminal;
            }
        }
        public SensorConfigurePage SensorConfigurePage { get; private set; }
        public SerialPortControlPage SerialPortControlPage { get; private set; }
        public GraphicGridPage GraphicGridPage { get; private set; }
        public MainWindow()
        {
            InitializeComponent();

            (SensorConfigureTab.Content as Frame).ContentRendered += SensorConfigureTabFrame_ContentRendered;
        }

        private void SensorConfigureTabFrame_ContentRendered(object sender, EventArgs e)
        {
            GraphicGridPage = (GraphicTab.Content as Frame).Content as GraphicGridPage;
            GraphicGridPage.VR33BTerminal = VR33BTerminal;

            SensorConfigurePage = (SensorConfigureTab.Content as Frame).Content as SensorConfigurePage;
            SensorConfigurePage.SettingViewModel.VR33BTerminal = VR33BTerminal;

            SerialPortControlPage = (SerialConfigureTab.Content as Frame).Content as SerialPortControlPage;
            SerialPortControlPage.ViewModel.VR33BTerminal = VR33BTerminal;

        }

        private void SerialPortControlPage_OnStateChanged(string stateMessage)
        {
            StateBlock.Text = stateMessage;
        }

        protected override void OnClosed(EventArgs e)
        {
            var pcSerialSetting = PCSerialPortSetting.FromSerialPort(VR33BTerminal.SerialPort);
            var fileName = PCSerialPortSettingName;
            var filePath = Environment.CurrentDirectory + "//" + fileName;
            XmlSerializer serializer = new XmlSerializer(typeof(PCSerialPortSetting));
            using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                serializer.Serialize(settingStream, pcSerialSetting);
            };
        }
    }

    public struct PCSerialPortSetting
    {
        /*
        SerialPort.BaudRate = 115200;
            SerialPort.StopBits = StopBits.One;
            SerialPort.DataBits = 8;
            SerialPort.Handshake = Handshake.None;
            SerialPort.RtsEnable = true;
            */
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public StopBits StopBits { get; set; }
        public int DataBits { get; set; }
        public Handshake HandShake { get; set; }
        public bool RtsEnable { get; set; }

        public static PCSerialPortSetting FromSerialPort(SerialPort serialPort)
        {
            return new PCSerialPortSetting
            {
                PortName = serialPort.PortName,
                BaudRate = serialPort.BaudRate,
                StopBits = serialPort.StopBits,
                DataBits = serialPort.DataBits,
                HandShake = serialPort.Handshake,
                RtsEnable = serialPort.RtsEnable
            };
        }
    }
}
