using System;
using System.Collections.Generic;
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

        public VR33BSampleListControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _VR33BTerminal.VR33BSampleDataStorage.Updated += VR33BSampleDataStorage_Updated;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _VR33BTerminal.VR33BSampleDataStorage.Updated -= VR33BSampleDataStorage_Updated;
        }

        private void VR33BSampleDataStorage_Updated(object sender, VR33BSampleValue e)
        {
            throw new NotImplementedException();
        }

        
    }

    internal class VR33BSampleListControlViewModel: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private int _MinFilterIndex;
        public int MinFilterIndex
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

        private int _MaxFilterIndex;
        public int MaxFilterIndex
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
        public VR33BSampleListControlViewModel()
        {
            MinFilterIndex = 0;
            MaxFilterIndex = 100;
        }

        
    }
}
