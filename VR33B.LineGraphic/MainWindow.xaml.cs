﻿using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace VR33B.LineGraphic
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        VR33BTerminal VR33BTerminal;
        ObservableCollection<string> SendDataStrs;
        ObservableCollection<string> ReceiveDataStrs;
        ObservableCollection<VR33BAccelerometerRange> SetAccRangeComboBoxSource;
        
        public MainWindow()
        {
            InitializeComponent();
            SendDataStrs = new ObservableCollection<string>();
            SendCommandListBox.ItemsSource = SendDataStrs;
            SendDataStrs.Add("sdfas");
            SendDataStrs.Add("sdf");
            ReceiveDataStrs = new ObservableCollection<string>();
            ReceiveCommandListBox.ItemsSource = ReceiveDataStrs;
            ReceiveDataStrs.Add("HAHAHA");

            SetAccRangeComboBoxSource = new ObservableCollection<VR33BAccelerometerRange>() { VR33BAccelerometerRange._2g, VR33BAccelerometerRange._4g, VR33BAccelerometerRange._8g, VR33BAccelerometerRange._16g };
            SetAccRangeComboBox.ItemsSource = SetAccRangeComboBoxSource;



            VR33BTerminal = new VR33BTerminal();
            VR33BTerminal.OnReceived += VR33BTerminal_OnReceived;

            VR33BTerminal.OnSerialPortSent += VR33BTerminal_OnSerialPortSent;
            VR33BTerminal.OnVR33BSampleValueReceived += VR33BTerminal_OnVR33BSampleValueReceived;

            double[] x = new double[200];
            for (int i = 0; i < x.Length; i++)
                x[i] = 3.1415 * i / (x.Length - 1);
            double[] size = new double[200];
            for (int i = 0; i < size.Length; i++)
                size[i] = 4;
            for (int i = 0; i < 3; i++)
            {
                var lg = new LineGraph();
                var cg = new CircleMarkerGraph();

                Lines.Children.Add(lg);
                lg.Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                if (i == 0)
                {
                    lg.Description = cg.Description = "Axis-X";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Red);
                }
                if (i == 1)
                {
                    lg.Description = cg.Description = "Axis-Y";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Green);
                }
                if (i == 2)
                {
                    lg.Description = cg.Description = "Axis-Z";
                    lg.Stroke = cg.Stroke = new SolidColorBrush(Colors.Blue);
                }
                lg.StrokeThickness = 2;
                lg.Plot(x, x.Select(v => Math.Sin(v + i / 10.0)).ToArray());

                Lines.Children.Add(cg);

                //cg.PlotSize (x, x.Select(v => Math.Sin(v + i / 10.0)), size);
                cg.PlotXY(x, x.Select(v => Math.Sin(v + i / 10.0)));
                cg.Size = size;
                //cg.Size = size.ConvertAll(v => { return 100.0; }).ToArray();
                //cg.MarkersBatchSize = 2;
            }
        }

        private void VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            System.Diagnostics.Debug.WriteLine(e);
        }

        private void VR33BTerminal_OnSerialPortSent(object sender, VR33BSendData e)
        {
            this.Dispatcher.Invoke(new Action(() => {
                SendDataStrs.Add(e.ToString());
                
            }));
            
        }

        private void VR33BTerminal_OnReceived(object sender, VR33BReceiveData e)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                ReceiveDataStrs.Add(e.ToString());
            }));
            
        }

        private void OpenSerialPortBtn_Click(object sender, RoutedEventArgs e)
        {
            SendDataStrs.Clear();
            //SendCommandListBox.ItemsSource = SendDataStrs;
            ReceiveDataStrs.Clear();
            try
            {
                VR33BTerminal.SerialPort.Open();
            }
            catch(Exception)
            {
                System.Diagnostics.Debug.WriteLine("PORT IS USING");
            }
            
        }

        private async void SendTestMsgBtn_Click(object sender, RoutedEventArgs e)
        {
            if(VR33BTerminal.SerialPort.IsOpen)
            {
                //VR33BTerminal.SerialPort.Write(new byte[] { 0xff, 0x03, 0x00, 0x01, 0x00, 0x01, 0xc0, 0x14 }, 0, 8);
                //VR33BTerminal.Send(false ,0x01, true, new byte[] { 0, 1 });

                VR33BSendData sendData = new VR33BSendData
                {
                    DeviceAddress = 0xff,
                    ReadOrWrite = VR33BMessageType.Read,
                    RegisterAddress = 0x01,
                    Data = new byte[] { 0, 1 }
                };
                //VR33BTerminal.Send(sendData);
                var response1 = await VR33BTerminal.SendCommandAsync(new ReadAddressCommand());
                if (response1.Success)
                {
                    System.Diagnostics.Debug.WriteLine("1:" + response1.Response);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("1:Failed");
                }
            }
        }

        private async void ReadAccRangeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (VR33BTerminal.SerialPort.IsOpen)
            {
                var response1 = await VR33BTerminal.SendCommandAsync(new ReadAccelerometerRange(VR33BTerminal));
                //var response2 = await VR33BTerminal.SendCommandAsync(new ReadAddressCommand());
                //var response3 = await VR33BTerminal.SendCommandAsync(new ReadAccelerometerRange(VR33BTerminal));
                if (response1.Success)
                {
                    System.Diagnostics.Debug.WriteLine("1:"+response1.Response);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("1:Failed");
                }
            }
                
        }

        private async void SetAccRangeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!VR33BTerminal.SerialPort.IsOpen)
            {
                return;
            }
            var selectedItem = (VR33BAccelerometerRange)SetAccRangeComboBox.SelectedItem;
            var response = await VR33BTerminal.SendCommandAsync(new SetAccelerometerRangeCommand(VR33BTerminal, selectedItem));
            if (response.Success)
            {
                System.Diagnostics.Debug.WriteLine(response.Response);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Failed");
            }
        }

        private async void ReadAccmeterButton_Click(object sender, RoutedEventArgs e)
        {
            if(!VR33BTerminal.SerialPort.IsOpen)
            {
                return;
            }
            await VR33BTerminal.SendCommandAsync(new StartSampleCommand(VR33BTerminal));
        }

        private async void SamplingCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (VR33BTerminal.SerialPort.IsOpen)
            {
                await VR33BTerminal.StartSampleAsync();
            }
        }

        private async void SamplingCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if(VR33BTerminal.SerialPort.IsOpen)
            {
                await VR33BTerminal.StopSampleAsync();
            }
        }
    }
}
