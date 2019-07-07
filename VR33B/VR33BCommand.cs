using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public class CalibrateXCommand : ICommand
    {
        public int MaximumRepeatCount
        {
            get
            {
                return 0;
            }
        }

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence { get; }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            return true;
        }

        public bool OnTimeout()
        {
            return true;
        }

        public CalibrateXCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0113,
                Data = new byte[] { 0, 00 }
            };
            SendDataSequence = new (VR33BSendData, TimeSpan)[]{ (sendData, TimeSpan.FromMilliseconds(50))};
        }
    }

    public class CalibrateYCommand : ICommand
    {
        public int MaximumRepeatCount
        {
            get
            {
                return 0;
            }
        }

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence { get; }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            return true;
        }

        public bool OnTimeout()
        {
            return true;
        }

        public CalibrateYCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0114,
                Data = new byte[] { 0, 00 }
            };
            SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, TimeSpan.FromMilliseconds(50)) };
        }
    }

    public class CalibrateZCommand : ICommand
    {
        public int MaximumRepeatCount
        {
            get
            {
                return 0;
            }
        }

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence { get; }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            return true;
        }

        public bool OnTimeout()
        {
            return true;
        }

        public CalibrateZCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0115,
                Data = new byte[] { 0, 00 }
            };
            SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, TimeSpan.FromMilliseconds(50)) };
        }
    }

    public class ResetCommand : ICommand
    {
        public int MaximumRepeatCount => throw new NotImplementedException();

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence => throw new NotImplementedException();

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            throw new NotImplementedException();
        }

        public bool OnTimeout()
        {
            throw new NotImplementedException();
        }

        public ResetCommand(VR33BTerminal vr33bTerminal)
        {
            /*
            var resetSendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x28,
                Data = new byte[] { 0x55, 0xaa }
            };
            */
            

        }
    }

    public class ReadAllSettingCommand : ICommand
    {

        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }

        private readonly (VR33BSendData, TimeSpan)[] _SendDataSequence;

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence
        {
            get
            {
                return _SendDataSequence;
            }
        }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 0x17)
            {
                return true;
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public ReadAllSettingCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0015,
                Data = new byte[] { 0, 0x0c }
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 100)) };
        }
    }

    public class SetThresholdInPercentCommand : ICommand
    {
        public int ThresholdInPercent { get; set; }
        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }

        private readonly (VR33BSendData, TimeSpan)[] _SendDataSequence;

        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence
        {
            get
            {
                return _SendDataSequence;
            }
        }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 2)
            {
                if (receiveData.Data[0] == (byte)ThresholdInPercent)
                {
                    return true;
                }
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public SetThresholdInPercentCommand(VR33BTerminal vr33bTerminal, int thresholdInPercent)
        {
            ThresholdInPercent = thresholdInPercent;
            var setData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0008,
                Data = new byte[] {0, (byte)thresholdInPercent}
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[]
            {
                (setData, new TimeSpan(0, 0, 0, 0, 100)),
            };
        }
    }
}
