using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    [Serializable()]
    public struct VR33BSampleValue
    {
        /// <summary>
        /// 在一次采样中，采样点按顺序产生，并且按顺序赋予一个采样编号
        /// </summary>
        public long SampleIndex { get; set; }
        public DateTime SampleDateTime { get; set; }
        public (UInt16 X, UInt16 Y, UInt16 Z) RawAccelerometerValue { get; set; }
        public UInt16 RawTemperature { get;  set; }
        public UInt16 RawHumidity { get; set; }

        public Vector3 AccelerometerValue
        {
            get
            {
                return new Vector3((Int16)(RawAccelerometerValue.X) / 1000.0f, (Int16)(RawAccelerometerValue.Y) / 1000.0f, (Int16)(RawAccelerometerValue.Z) / 1000.0f);
            }
        }
        public double Temperature
        {
            get
            {
                return RawTemperature / 100.0f;
            }
        }
        public double Humidity
        {
            get
            {
                return RawHumidity / 10.0f;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            sb.Append(SampleDateTime + "|");
            sb.Append(AccelerometerValue + "|");
            sb.Append("T:" + Temperature + "|");
            sb.Append("H:" + Humidity + "]");
            return sb.ToString();
            
        }

        public static VR33BSampleValue FromVR33BReceiveData(VR33BReceiveData receiveData, VR33BSetting vr33bSetting)
        {
            byte[] xBytes = new byte[] { receiveData.Data[1], receiveData.Data[0] };
            byte[] yBytes = new byte[] { receiveData.Data[3], receiveData.Data[2] };
            byte[] zBytes = new byte[] { receiveData.Data[5], receiveData.Data[4] };
            byte[] tBytes = new byte[] { receiveData.Data[7], receiveData.Data[6] };
            byte[] hBytes = new byte[] { receiveData.Data[9], receiveData.Data[8] };
            UInt16 rawX = BitConverter.ToUInt16(xBytes, 0);
            UInt16 rawY = BitConverter.ToUInt16(yBytes, 0);
            UInt16 rawZ = BitConverter.ToUInt16(zBytes, 0);
            UInt16 rawT = BitConverter.ToUInt16(tBytes, 0);
            UInt16 rawH = BitConverter.ToUInt16(hBytes, 0);
            VR33BSampleValue sampleValue = new VR33BSampleValue()
            {
                SampleDateTime = DateTime.Now,

                RawAccelerometerValue = (rawX, rawY, rawZ),
                RawTemperature = rawT,
                RawHumidity = rawH
            };
            return sampleValue;
        }



    }
}
