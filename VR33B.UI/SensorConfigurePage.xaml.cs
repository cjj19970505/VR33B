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
    /// SensorConfigurePage.xaml 的交互逻辑
    /// </summary>
    public partial class SensorConfigurePage : Page
    {
        public SensorConfigurePage()
        {
            InitializeComponent();
            SensorBaudRateBox.ItemsSource = baudRates;
            SensorBaudRateBox.SelectedItem = baudRates[0];
            SensorStopBitBox.ItemsSource = Enum.GetValues(typeof(StopBits));
            SensorStopBitBox.SelectedItem = StopBits.One;
            SensorParityBox.ItemsSource = Enum.GetValues(typeof(Parity));
            SensorParityBox.SelectedItem = Parity.None;
        }

        private ObservableCollection<int> baudRates = new ObservableCollection<int> { 9600 };
        private ObservableCollection<int> dataBits = new ObservableCollection<int> { 8, 7, 6 };

        private void SamplingThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SamplingThresholdValueBlock.Text = ((int)e.NewValue).ToString() + "%";
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            SamplingThresholdSlider.Width = SamplingThresholdColumn.ActualWidth - SamplingThresholdValueBlock.ActualWidth - 15;
        }
    }
}
