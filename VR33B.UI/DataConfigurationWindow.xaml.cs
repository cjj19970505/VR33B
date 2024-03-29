﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VR33B.UI
{
    /// <summary>
    /// DataConfigurationWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DataConfigurationWindow : Window
    {
        public DateTime SelectedDateTime { get; private set; }
        public DataConfigurationWindow()
        {
            InitializeComponent();
            CombinedClock.Time = DateTime.Now;
            CombinedCalendar.SelectedDate = DateTime.Now;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var date = CombinedCalendar.SelectedDate.Value;
            var time = CombinedClock.Time;
            SelectedDateTime = time;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
