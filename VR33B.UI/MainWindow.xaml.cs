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

namespace VR33B.UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private static VR33BTerminal _VR33BTerminal;
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                if(_VR33BTerminal == null)
                {
                    _VR33BTerminal = new VR33BTerminal(new VR33BSqliteStorage(), false);
                    (_VR33BTerminal.VR33BSampleDataStorage as VR33BSqliteStorage).SampleTimeDispatcher = new VR33BSampleTimeDispatcher(_VR33BTerminal);

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
    }
}
