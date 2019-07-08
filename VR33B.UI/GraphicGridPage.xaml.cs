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
using VR33B.LineGraphic;

namespace VR33B.UI
{
    /// <summary>
    /// GraphicGridPage.xaml 的交互逻辑
    /// </summary>
    public partial class GraphicGridPage : Page
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
                _VR33BTerminal = value;
                ViewModel.VR33BTerminal = value;
                VR33BOxyPlotControl.VR33BTerminal = _VR33BTerminal;
                SampleListControl.VR33BTerminal = _VR33BTerminal;
                
            }
        }
        public GraphicGridPage()
        {
            InitializeComponent();
            SampleListControl.ViewModel.OxyPlotControl = VR33BOxyPlotControl;
        }

        private void SampleListControl_OnSampleValueSelectionChanged(object sender, VR33BSampleValue? e)
        {
            if(e == null)
            {

            }
            else
            {
                var sampleValue = e.Value;
                VR33BOxyPlotControl.TrackingModeOn = false;
                VR33BOxyPlotControl.Indicate(sampleValue.SampleDateTime);
            }

        }
    }

    internal class GraphicGridViewModel
    {
        public VR33BTerminal VR33BTerminal { get; set; }
        public bool Sampling { get; set; }
    }
}
