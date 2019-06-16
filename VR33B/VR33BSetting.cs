using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public enum VR33BAccelerometerRange { _2g = 0x02, _4g = 0x04, _8g = 0x08, _16g = 0x16 };
    public enum VR33BSerialPortBaudRate { _9600 = 0x01, _115200 = 0x02, _256000 = 0x03}

    public enum VR33BSerialPortParity { None = 0x01, Even = 0x02, Odd = 0x03 }
    public enum VR33SerialPortStopBits { One = 0x01, Two = 0x02}

    public enum VR33BSampleFrequence { _1Hz = 0x01, _5Hz = 0x05, _20Hz = 0x20, _50Hz = 0x50, _100Hz = 0x100, _200Hz = 0x200}
    public struct VR33BSetting
    {
        public byte DeviceAddress { get; internal set; }
        public VR33BSerialPortBaudRate SerialPortBaudRate { get; set; }
        public VR33BSerialPortParity SerialPortParity { get; set; }
        public VR33SerialPortStopBits SerialPortStopbits { get; set; }
        public VR33BSampleFrequence SampleFrequence { get; set; }
        public VR33BAccelerometerRange AccelerometerRange { get; set; }
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerZero { get; set; }
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerSensibility { get; set; }
    }
}
