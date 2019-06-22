﻿using System;
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
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return (VR33BTerminal)DataContext;
            }
        }
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

        private VR33BSampleFrequence _ComboxItemToVR33BSampleFrequence(ComboBoxItem comboBoxItem)
        {
            switch(comboBoxItem.Tag)
            {
                case "1Hz":
                    return VR33BSampleFrequence._1Hz;
                case "5Hz":
                    return VR33BSampleFrequence._5Hz;
                case "20Hz":
                    return VR33BSampleFrequence._20Hz;
                case "50Hz":
                    return VR33BSampleFrequence._50Hz;
                case "100Hz":
                    return VR33BSampleFrequence._100Hz;
                case "200Hz":
                    return VR33BSampleFrequence._200Hz;
                default:
                    return VR33BSampleFrequence._1Hz;
            }
        }

        private async void SamplingRateBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count == 0)
            {
                return;
            }
            var removeComboBoxItem = e.RemovedItems[0] as ComboBoxItem;
            var addedComBoxItem = e.AddedItems[0] as ComboBoxItem;
            var removeFrequence = _ComboxItemToVR33BSampleFrequence(removeComboBoxItem);
            var addedFrequence = _ComboxItemToVR33BSampleFrequence(addedComBoxItem);
            SamplingRateRing.Visibility = Visibility.Visible;
            var response = await VR33BTerminal.SetSampleFrequencyAsync(addedFrequence);
            await Dispatcher.InvokeAsync(() => { SamplingRateRing.Visibility = Visibility.Collapsed; });
        }
    }
}
