using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// VR33BOxyLegendControl.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BOxyLegendControl : UserControl
    {
        public LineSeries BindingLineSeries
        {
            get
            {
                return (LineSeries)DataContext;
            }
        }
        public VR33BOxyLegendControl()
        {
            InitializeComponent();
            
        }

        private void MarkerSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(BindingLineSeries != null)
            {
                BindingLineSeries.MarkerSize = e.NewValue;
                BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
            }
        }

        private void LineStrokeSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BindingLineSeries != null)
            {
                BindingLineSeries.StrokeThickness = e.NewValue;
                BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
            }
        }

        private void LineColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            bool ok = ColorPickerWPF.ColorPickerWindow.ShowDialog(out Color color);
            if (ok && BindingLineSeries!=null)
            {
                BindingLineSeries.Color = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
                (sender as Button).Background = new SolidColorBrush(color);
                (sender as Button).Foreground = new SolidColorBrush(color);
                LineIcon.Stroke = new SolidColorBrush(color);
                BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
                
            }
        }

        private void MarkerColorPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            bool ok = ColorPickerWPF.ColorPickerWindow.ShowDialog(out Color color);
            if (ok && BindingLineSeries != null)
            {
                BindingLineSeries.MarkerFill = OxyColor.FromArgb(color.A, color.R, color.G, color.B);
                (sender as Button).Background = new SolidColorBrush(color);
                (sender as Button).Foreground = new SolidColorBrush(color);
                MarkerIcon.Stroke = MarkerIcon.Fill = new SolidColorBrush(color);
                BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(!(e.NewValue is LineSeries))
            {
                return;
            }
            LineSeries bindingLineSeries = (LineSeries)e.NewValue;
            if(bindingLineSeries == null)
            {
                return;
            }
            DisplayCheckBox.IsChecked = bindingLineSeries.IsVisible;
            DisplayCheckBox.Content = bindingLineSeries.Title;

            LineColorPickerBtn.Background = LineColorPickerBtn.Foreground = new SolidColorBrush(bindingLineSeries.Color.ToColor());
            LineIcon.Stroke = new SolidColorBrush(bindingLineSeries.Color.ToColor());
            LineStrokeSizeSlider.Value = bindingLineSeries.StrokeThickness;
            
            MarkerColorPickerBtn.Background = MarkerColorPickerBtn.Foreground = new SolidColorBrush(bindingLineSeries.MarkerFill.ToColor());
            MarkerIcon.Stroke = MarkerIcon.Fill = new SolidColorBrush(bindingLineSeries.Color.ToColor());
            MarkerSizeSlider.Value = bindingLineSeries.StrokeThickness;
        }

        private void DisplayCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(BindingLineSeries != null)
            {
                BindingLineSeries.IsVisible = true;
                BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
            }
        }

        private void DisplayCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingLineSeries.IsVisible = false;
            BindingLineSeries.PlotModel.PlotView.InvalidatePlot(false);
        }
    }

    public class OxyColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            OxyColor oxyColor = (OxyColor)value;
            SolidColorBrush brush = new SolidColorBrush(oxyColor.ToColor());
            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = (SolidColorBrush)value;
            return brush.Color.ToOxyColor();
        }
    }
}
