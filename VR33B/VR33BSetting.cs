using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public enum VR33BAccelerometerRange { [Description("2g")]_2g = 0x02, [Description("4g")]_4g = 0x04, [Description("8g")]_8g = 0x08, [Description("16g")]_16g = 0x16 };
    public enum VR33BSerialPortBaudRate {[Description("9600")] _9600 = 0x01, [Description("115200")] _115200 = 0x02, [Description("256000")] _256000 = 0x03}

    public enum VR33BSerialPortParity { None = 0x01, Even = 0x02, Odd = 0x03 }
    public enum VR33SerialPortStopBits { One = 0x01, Two = 0x02}

    public enum VR33BSampleFrequence { [Description("1Hz")]_1Hz = 0x01, [Description("5Hz")] _5Hz = 0x05, [Description("20Hz")] _20Hz = 0x20, [Description("50Hz")] _50Hz = 0x50, [Description("100Hz")] _100Hz = 0x100, [Description("200Hz")] _200Hz = 0x200}
    public class VR33BSetting
    {
        public event EventHandler<byte> OnDeviceAddressChanged;
        public event EventHandler<VR33BSampleFrequence> OnSampleFrequencyChanged;
        public event EventHandler<VR33BAccelerometerRange> OnAccelerometerRangeChanged;
        private byte _DeviceAddress;
        public byte DeviceAddress
        {
            get
            {
                return _DeviceAddress;
            }
            set
            {
                _DeviceAddress = value;
                OnDeviceAddressChanged?.Invoke(this, _DeviceAddress);
            }
        }
        public VR33BSerialPortBaudRate SerialPortBaudRate { get; set; }
        public VR33BSerialPortParity SerialPortParity { get; set; }
        public VR33SerialPortStopBits SerialPortStopbits { get; set; }

        private VR33BSampleFrequence _SampleFrequence;
        public VR33BSampleFrequence SampleFrequence
        {
            get
            {
                return _SampleFrequence;
            }
            set
            {
                _SampleFrequence = value;
                OnSampleFrequencyChanged?.Invoke(this, _SampleFrequence);
            }
        }

        private VR33BAccelerometerRange _AccelerometerRange;
        public VR33BAccelerometerRange AccelerometerRange
        {
            get
            {
                return _AccelerometerRange;
            }
            set
            {
                _AccelerometerRange = value;
                OnAccelerometerRangeChanged?.Invoke(this, _AccelerometerRange);
            }
        }
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerZero { get; set; }
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerSensibility { get; set; }

        
    }
}
