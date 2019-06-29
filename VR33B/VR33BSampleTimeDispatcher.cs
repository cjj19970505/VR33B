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
                if (_VR33BTerminal != null)
                {
                    _VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
            }
        }

        private object _SampleValuesBufferLock;
        private List<VR33BSampleValue> _SampleValuesBuffer;




        /// <summary>
        /// 请不要在这里阻塞求你们了
        /// </summary>
        public event EventHandler<VR33BSampleValue> OnSampleValueTimeDispatched;

        public VR33BSampleTimeDispatcher(VR33BTerminal terminal)
        {
            VR33BTerminal = terminal;
            _SampleValuesBufferLock = new object();
            _SampleValuesBuffer = new List<VR33BSampleValue>();
        }

        private async void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            //OnSampleValueTimeDispatched?.Invoke(this, e);
            //return;
            lock (_SampleValuesBufferLock)
            {
                if (_SampleValuesBuffer.Count == 0 || _SampleValuesBuffer.Last().SampleDateTime.Second == e.SampleDateTime.Second)
                {
                    _SampleValuesBuffer.Add(e);
                    return;
                }
            }
            List<VR33BSampleValue> sameSecondSampleValues;
            lock (_SampleValuesBufferLock)
            {
                sameSecondSampleValues = _SampleValuesBuffer.ToList();
                _SampleValuesBuffer.Clear();
                _SampleValuesBuffer.Add(e);
            }

            await Task.Run(() =>
            {
                double addTimeSpanInMs = 1000.0 / sameSecondSampleValues.Count;
                VR33BSampleValue firstInCurrentSecond = sameSecondSampleValues.First();
                for (int i = 1; i < sameSecondSampleValues.Count; i++)
                {
                    var sampleValue = sameSecondSampleValues[i];
                    sampleValue.SampleDateTime = firstInCurrentSecond.SampleDateTime.AddMilliseconds((sampleValue.SampleIndex - firstInCurrentSecond.SampleIndex) * addTimeSpanInMs);
                    sameSecondSampleValues[i] = sampleValue;
                }

                for(int i = 1; i < sameSecondSampleValues.Count; i++)
                {
                    var sampleValue = sameSecondSampleValues[i];
                }

                foreach (var sampleValue in sameSecondSampleValues)
                {
                    OnSampleValueTimeDispatched?.Invoke(this, sampleValue);
                }
            });
        }


    }
}
