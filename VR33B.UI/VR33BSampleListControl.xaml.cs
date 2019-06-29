using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                    _VR33BTerminal.VR33BSampleDataStorage.Updated -= VR33BSampleDataStorage_Updated;
                }
                _VR33BTerminal = value;
                if(IsLoaded)
                {
                    _VR33BTerminal.VR33BSampleDataStorage.Updated += VR33BSampleDataStorage_Updated;
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
        public event EventHandler FilterChanged;
        
        
        public VR33BSampleListControl()
        {
            InitializeComponent();
            FilterChanged += VR33BSampleListControl_FilterChanged;
        }

        private async void VR33BSampleListControl_FilterChanged(object sender, EventArgs e)
        {
            ViewModel.DataGridItemSource.Clear();

            long minIndex = ViewModel.MinFilterIndex;

            var indexFilteredSampleValues = await _VR33BTerminal.VR33BSampleDataStorage.GetFromSampleIndexRangeAsync(minIndex, ViewModel.MaxFilterIndex);
            var filteredSampleValues = new List<VR33BSampleValue>();
            foreach(var sampleValue in indexFilteredSampleValues)
            {
                if(await _FilterAsync(sampleValue))
                {
                    filteredSampleValues.Add(sampleValue);
                }
            }
            if (filteredSampleValues.Count > 0)
            {
                _LatestAddedToDataGridSampleValue = filteredSampleValues.Last();

                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var value in filteredSampleValues)
                    {
                        ViewModel.DataGridItemSource.Add(value);
                    }
                });
            }
        }
        private VR33BSampleValue _LatestAddedToDataGridSampleValue;
        private async void VR33BSampleDataStorage_Updated(object sender, VR33BSampleValue e)
        {
            bool filterResult = true;
            if(e.SampleIndex == 0)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    ViewModel.DataGridItemSource.Clear();
                });
            }
            if (ViewModel.DataGridItemSource.Count > 0)
            {
                filterResult = _Filter(_LatestAddedToDataGridSampleValue);
            }
            if(DateTime.Now - _LatestUpdataDataGridDateTime > _UpdateInterval && filterResult)
            {
                _LatestUpdataDataGridDateTime = DateTime.Now;
                long minIndex = ViewModel.MinFilterIndex;
                if(ViewModel.DataGridItemSource.Count > 0)
                {
                    minIndex = Math.Max(ViewModel.MinFilterIndex, _LatestAddedToDataGridSampleValue.SampleIndex + 1);
                }
                
                var indexFilteredSampleValues = await _VR33BTerminal.VR33BSampleDataStorage.GetFromSampleIndexRangeAsync(minIndex, ViewModel.MaxFilterIndex);
                var filteredSampleValues = (from sampleValue in indexFilteredSampleValues
                                           where _Filter(sampleValue)
                                           select sampleValue).ToArray();
                if(filteredSampleValues.Length > 0)
                {
                    _LatestAddedToDataGridSampleValue = filteredSampleValues.Last();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        
                        foreach (var value in filteredSampleValues)
                        {
                            if(ViewModel.DataGridItemSource.Count > 0 && ViewModel.DataGridItemSource.Last().SampleIndex >= value.SampleIndex)
                            {
                                
                            }
                            else
                            {
                                ViewModel.DataGridItemSource.Add(value);
                            }
                            
                        }
                    });
                }
                
            }
            
        }

        private bool _Filter(VR33BSampleValue sampleValue)
        {
            bool indexRangeFilter = false;
            if(sampleValue.SampleIndex >= ViewModel.MinFilterIndex && sampleValue.SampleIndex <= ViewModel.MaxFilterIndex)
            {
                indexRangeFilter = true;
            }
            bool overLoadFilter = true;
            if(sampleValue.SampleIndex < 3 && ViewModel.OverloadFilter)
            {
                overLoadFilter = false;
            }
            if(sampleValue.SampleIndex > 3 && ViewModel.OverloadFilter)
            {
                var queryTask = VR33BTerminal.VR33BSampleDataStorage.GetFromSampleIndexRangeAsync(sampleValue.SampleIndex - 2, sampleValue.SampleIndex - 1);
                queryTask.Wait();
                var result = queryTask.Result;
                TimeSpan ts1 = result[1].SampleDateTime - result[0].SampleDateTime;
                TimeSpan ts2 = sampleValue.SampleDateTime - result[1].SampleDateTime;
                if(ts1.TotalMilliseconds > ts2.TotalMilliseconds*1.5)
                {
                    overLoadFilter = true;
                }
                else
                {
                    overLoadFilter = false;
                }
            }

            return indexRangeFilter && overLoadFilter;
        }

        private async Task<bool> _FilterAsync(VR33BSampleValue sampleValue)
        {
            bool indexRangeFilter = false;
            if (sampleValue.SampleIndex >= ViewModel.MinFilterIndex && sampleValue.SampleIndex <= ViewModel.MaxFilterIndex)
            {
                indexRangeFilter = true;
            }
            bool overLoadFilter = true;
            if (sampleValue.SampleIndex < 3 && ViewModel.OverloadFilter)
            {
                overLoadFilter = false;
            }
            if (sampleValue.SampleIndex > 3 && ViewModel.OverloadFilter)
            {
                var result = await VR33BTerminal.VR33BSampleDataStorage.GetFromSampleIndexRangeAsync(sampleValue.SampleIndex - 2, sampleValue.SampleIndex - 1);
                TimeSpan ts1 = result[1].SampleDateTime - result[0].SampleDateTime;
                TimeSpan ts2 = sampleValue.SampleDateTime - result[1].SampleDateTime;
                if (ts1.TotalMilliseconds > ts2.TotalMilliseconds * 1.5)
                {
                    overLoadFilter = true;
                }
                else
                {
                    overLoadFilter = false;
                }
            }

            return indexRangeFilter && overLoadFilter;
        }

        private void SampleDataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if(e.AddedCells.Count>0)
            {
                OnSampleValueSelectionChanged?.Invoke(this, (VR33BSampleValue)e.AddedCells[0].Item);
            }
            
        }

        private void MinIndexFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var parseSuccess = long.TryParse(MinIndexFilterTextBox.Text, out long minIndex);
            if(parseSuccess)
            {
                ViewModel.MinFilterIndex = minIndex;
            }
            FilterChanged?.Invoke(this, null);
        }

        private void MaxIndexFilterTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var parseSuccess = long.TryParse(MaxIndexFilterTextBox.Text, out long maxIndex);
            if (parseSuccess)
            {
                ViewModel.MaxFilterIndex = maxIndex;
            }
            FilterChanged?.Invoke(this, null);
        }

        private void OverloadCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FilterChanged?.Invoke(this, null);
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if((bool)e.NewValue)
            {
                _VR33BTerminal.VR33BSampleDataStorage.Updated += VR33BSampleDataStorage_Updated;
            }
            else
            {
                _VR33BTerminal.VR33BSampleDataStorage.Updated -= VR33BSampleDataStorage_Updated;
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
        public VR33BSampleListControlViewModel()
        {
            MinFilterIndex = 0;
            MaxFilterIndex = 1000;
            DataGridItemSource = new ObservableCollection<VR33BSampleValue>();
            _DataGridItemSourceLock = new object();
        }

        
    }
}
