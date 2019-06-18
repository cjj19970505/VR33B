using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    /// <summary>
    /// this uses Sqlite to store the sample data and distribute them to wherever they are needed.
    /// It stores all the data into sqlite database and store only  a small amount of data in local memory
    /// the SampleProcessInfo Table stores every sample process info in the database, just like header of a file
    /// </summary>
    public class VR33BSampleDataStorage
    {
        private VR33BTerminal _VR33BTerminal;
        private SQLiteConnection _DBConnection;
        private object _SampleValuesLock;
        private List<VR33BSampleValue> _SampleValues;
        public event EventHandler<VR33BSampleValue> Updated;
        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            set
            {
                if(_VR33BTerminal != null)
                {
                    _VR33BTerminal.OnVR33BSampleStarted -= _VR33BTerminal_OnVR33BSampleStarted;
                    _VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                    _VR33BTerminal.OnVR33BSampleEnded -= _VR33BTerminal_OnVR33BSampleEnded;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
            }
        }

        public VR33BSampleDataStorage(VR33BTerminal vr33bTerminal)
        {
            _SampleValues = new List<VR33BSampleValue>();
            _SampleValuesLock = new object();
            VR33BTerminal = vr33bTerminal;
        }

        private void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            
        }

        private void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            lock(_SampleValuesLock)
            {
                _SampleValues.Add(e);
            }
            Updated?.Invoke(this, e);
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, EventArgs e)
        {
            lock(_SampleValuesLock)
            {
                _SampleValues.Clear();
            }
        }

        public Task<List<VR33BSampleValue>> GetFromDateTimeRange(DateTime startDateTime, DateTime endDateTime)
        {
            return Task.Run(() =>
            {
                lock (_SampleValuesLock)
                {
                    var query = (from sampleValue in _SampleValues
                                where sampleValue.SampleDateTime >= startDateTime && sampleValue.SampleDateTime <= endDateTime
                                select sampleValue).ToList();
                    return query;
                }
            });
            
        }

        

        
    }
}
