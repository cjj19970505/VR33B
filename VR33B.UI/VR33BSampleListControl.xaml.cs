using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using VR33B.LineGraphic;

namespace VR33B.UI
{
    /// <summary>
    /// VR33BSampleListControl.xaml 的交互逻辑
    /// </summary>
    public partial class VR33BSampleListControl : UserControl
    {
        private VR33BTerminal _VR33BTerminal;
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            set
            {
                if(_VR33BTerminal == null && IsLoaded)
                {
                }
                _VR33BTerminal = value;
                if(IsLoaded)
                {
                }
                
            }
        }
        public VR33BOxyPlotControl PlotControl { get; set; }

        private TimeSpan _UpdateInterval
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 200);
            }
        }
        private DateTime _LatestUpdataDataGridDateTime;

        public event EventHandler<VR33BSampleValue?> OnSampleValueSelectionChanged;
        
        
        public VR33BSampleListControl()
        {
            InitializeComponent();
        }

        private void SampleDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if(e.AddedCells.Count>0)
            {
                OnSampleValueSelectionChanged?.Invoke(this, (VR33BSampleValue)e.AddedCells[0].Item);
            }
            
        }

        private async void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var queryResult = await VR33BTerminal.VR33BSampleDataStorage.QueryAsync(ViewModel.GetFilteredEnumerable);
            ViewModel.DataGridItemSource.Clear();
            foreach(var sampleValue in queryResult)
            {
                ViewModel.DataGridItemSource.Add(sampleValue);
            }
        }

        private void IndexFilterTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
            Regex addTextMatch = new Regex(@"^[0-9]+$");
            if (!addTextMatch.IsMatch(e.Text))
            {
                e.Handled = true;
            }
            
        }

        private void TimeSpanTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            
            if ((sender as TextBox).Text.Contains("."))
            {
                if(e.Text.Contains("."))
                {
                    e.Handled = true;
                    return;
                }
            }
            Regex addTextMatch = new Regex(@"^[0-9.]+$");
            if (!addTextMatch.IsMatch(e.Text))
            {
                e.Handled = true;
            }
        }
    }

    internal class VR33BSampleListControlViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        public object _DataGridItemSourceLock;
        public ObservableCollection<VR33BSampleValue> DataGridItemSource { get; set; }
        private long _MinFilterIndex;
        public long MinFilterIndex
        {
            get
            {
                return _MinFilterIndex;
            }
            set
            {
                _MinFilterIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinFilterIndex"));

            }
        }
        private long _MaxFilterIndex;
        public long MaxFilterIndex
        {
            get
            {
                return _MaxFilterIndex;
            }
            set
            {
                _MaxFilterIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxFilterIndex"));
            }
        }
        private bool _OverloadFilter;
        public bool OverloadFilter
        {
            get
            {
                return _OverloadFilter;
            }
            set
            {
                _OverloadFilter = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("OverloadFilter"));
            }
        }

        private bool _FilterFromIndex;
        public bool FilterFromIndex
        {
            get
            {
                return _FilterFromIndex;
            }
            set
            {
                _FilterFromIndex = value;
                if(value)
                {
                    FilterFromRectBar = false;
                }
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilterFromIndex"));
            }
        }

        private bool _FilterFromRectBar;
        public bool FilterFromRectBar
        {
            get
            {
                return _FilterFromRectBar;
            }
            set
            {
                _FilterFromRectBar = value;
                if(value)
                {
                    FilterFromIndex = false;
                }
                
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilterFromRectBar"));
            }
        }

        private bool _FilterFromSampleTimeSpan;
        public bool FilterFromSampleTimeSpan
        {
            get
            {
                return _FilterFromSampleTimeSpan;
            }
            set
            {
                _FilterFromSampleTimeSpan = value;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FilterFromSampleTimeSpan"));
            }
        }

        public double _MinSampleTimeSpan;
        public double MinSampleTimeSpan
        {
            get
            {
                return _MinSampleTimeSpan;
            }

            set
            {
                _MinSampleTimeSpan = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MinSampleTimeSpan"));
            }
        }

        public double _MaxSampleTimeSpan;
        public double MaxSampleTimeSpan
        {
            get
            {
                return _MaxSampleTimeSpan;
            }

            set
            {
                _MaxSampleTimeSpan = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MaxSampleTimeSpan"));
            }
        }

        public string _TestBoxBinding;
        public string TestBoxBinding
        {
            get
            {
                return _TestBoxBinding;
            }
            set
            {
                _TestBoxBinding = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("TestBoxBinding"));
            }
        }
        public VR33BOxyPlotControl OxyPlotControl { get; set; }

        public IEnumerable<VR33BSampleValue> GetFilteredEnumerable(IEnumerable<VR33BSampleValue> allSample)
        {
            var filtered = allSample;
            if(FilterFromIndex)
            {
                filtered = from sampleValue in filtered
                           where sampleValue.SampleIndex >= MinFilterIndex && sampleValue.SampleIndex <= MaxFilterIndex
                           select sampleValue;
            }
            if(_FilterFromSampleTimeSpan)
            {
                filtered = from sampleValue in filtered
                           where sampleValue.SampleTimeSpanInMs >= MinSampleTimeSpan && sampleValue.SampleTimeSpanInMs <= MaxSampleTimeSpan
                           select sampleValue;
            }
            if(_FilterFromRectBar)
            {
                filtered = from sampleValue in filtered
                           where sampleValue.SampleDateTime >= OxyPlotControl.SelectedDateTimeRange.Start && sampleValue.SampleDateTime <= OxyPlotControl.SelectedDateTimeRange.End
                           select sampleValue;
            }
            return filtered;
            
        }
        public VR33BSampleListControlViewModel()
        {
            MinFilterIndex = 0;
            MaxFilterIndex = 1000;
            DataGridItemSource = new ObservableCollection<VR33BSampleValue>();
            _DataGridItemSourceLock = new object();
        }
    }
    internal class LongToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            long number = (long)value;
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            bool canConvert = long.TryParse(str, out long number);
            if (canConvert)
            {
                return number;
            }
            else
            {
                return 0;
            }
        }
    }

    internal class DoubleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double number = (double)value;
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = (string)value;
            bool canConvert = double.TryParse(str, out double number);
            if (canConvert)
            {
                return number;
            }
            else
            {
                return 0;
            }
        }
    }
}
