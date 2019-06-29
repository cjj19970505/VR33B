using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    /// <summary>
    /// This class should be remove in the future version and millionsecond precision of Sample Value should be consider in ARM
    /// </summary>
    public class VR33BSampleTimeDispatcher
    {
        private VR33BTerminal _VR33BTerminal;
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            private set
            {
                if(_VR33BTerminal != null)
                {
                    _VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
            }
        }

        private List<VR33BSampleValue> _CurrentSecondSampleValues;


        /// <summary>
        /// 请不要在这里阻塞求你们了
        /// </summary>
        public event EventHandler<VR33BSampleValue> OnSampleValueTimeDispatched;

        public VR33BSampleTimeDispatcher(VR33BTerminal terminal)
        {
            VR33BTerminal = terminal;

            _CurrentSecondSampleValues = new List<VR33BSampleValue>();
        }

        private void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            if(_CurrentSecondSampleValues.Count == 0 || _CurrentSecondSampleValues.Last().SampleDateTime.Second == e.SampleDateTime.Second)
            {
                _CurrentSecondSampleValues.Add(e);
            }
            else
            {
                TimeSpan addTimeSpan = TimeSpan.FromSeconds(1.0 / _CurrentSecondSampleValues.Count);
                VR33BSampleValue firstInCurrentSecond = _CurrentSecondSampleValues.First();
                
                var timeDispatchedValues = _CurrentSecondSampleValues.ConvertAll((value) =>
                {
                    var sampleValue = value;
                    sampleValue.SampleDateTime = value.SampleDateTime.AddSeconds((value.SampleIndex - firstInCurrentSecond.SampleIndex) * addTimeSpan.TotalSeconds);
                    return sampleValue;
                });
                _CurrentSecondSampleValues.Clear();
                _CurrentSecondSampleValues.Add(e);
                foreach (var sampleValue in timeDispatchedValues)
                {
                    OnSampleValueTimeDispatched?.Invoke(this, sampleValue);
                }
            }
        }

        
    }
}
