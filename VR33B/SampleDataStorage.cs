using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public class SampleDataStorage
    {
        private VR33BTerminal _VR33BTerminal;
        private string _CurrentFileName;
        private FileStream _CurrentFileStream;

        private Queue<SampleDataPage> _UsingPages;
        private List<SampleDataPage> PageList;
        private SampleDataPage _UpdatingPage;

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
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
                
            }
        }

        private void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public SampleDataStorage(VR33BTerminal terminal)
        {
            _VR33BTerminal = terminal;
        }

        private void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            _UpdatingPage.AddSampleValue(e);
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, EventArgs e)
        {
            _CurrentFileName = Path.GetTempFileName();
            _CurrentFileStream = File.Open(_CurrentFileName, FileMode.Open, FileAccess.ReadWrite);
        }

        private void _OnUpdatingSampleDataPageFull()
        {
            string newPageFileName = Path.GetTempFileName();
            SampleDataPage page = new SampleDataPage(this, newPageFileName);
            
        }

        internal class SampleDataPage
        {
            
            /// <summary>
            /// Sorted by DateTime
            /// </summary>
            public List<VR33BSampleValue> SampleValueList { get; set; }
            private object SampleValueListLock;
            public DateTime StartDateTime { get; set; }
            public DateTime EndDateTime { get; set; }
            public string FilePath { get; set; }
            public SampleDataStorage SampleDataStorage { get; set; }

            /// <summary>
            /// Max count of sampleValue that store in this page
            /// </summary>
            public int MaxCount { get; set; }

            public SampleDataPage(SampleDataStorage storage, string filePath, int maxCount = 10000)
            {
                SampleDataStorage = storage;
                SampleValueListLock = new object();
                SampleValueList = new List<VR33BSampleValue>();
                FilePath = filePath;
                MaxCount = maxCount;
            }

            public void AddSampleValue(VR33BSampleValue sampleValue)
            {
                lock(SampleValueListLock)
                {
                    if (SampleValueList.Count <= 0)
                    {
                        StartDateTime = sampleValue.SampleDateTime;
                    }
                    EndDateTime = sampleValue.SampleDateTime;
                    SampleValueList.Add(sampleValue);
                    if (SampleValueList.Count >= MaxCount)
                    {

                    }
                }

            }
            
        }
    }

    


}
