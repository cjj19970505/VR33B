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
    public enum VR33BSerialPortBaudRate {[Description("9600")] _9600 = 0x01, [Description("115200")] _115200 = 0x02}

    public enum VR33BSerialPortParity { None = 0x01, Even = 0x02, Odd = 0x03 }
    public enum VR33SerialPortStopBits { One = 0x01, Two = 0x02}
    public enum VR33BWorkingMode { Response = 0x00, Continuous = 0x10}
    public enum VR33BSampleFrequence { [Description("1Hz")]_1Hz = 1, [Description("4Hz")] _4Hz = 4, [Description("16Hz")] _16Hz = 16, [Description("64Hz")] _64Hz = 64, [Description("128Hz")] _128Hz = 128, [Description("256Hz")] _256Hz = 256}
    public class VR33BSetting
    {
        public event EventHandler<byte> OnDeviceAddressChanged;
        public event EventHandler<VR33BSampleFrequence> OnSampleFrequencyChanged;
        public event EventHandler<VR33BAccelerometerRange> OnAccelerometerRangeChanged;
        public event EventHandler<int> OnThresholdInPercentChanged;
        public event EventHandler<double> OnThresholdChanged;
        public event EventHandler<(UInt16 X, UInt16 Y, UInt16 Z)> OnAccelerometerZeroChanged;
        public event EventHandler<(UInt16 X, UInt16 Y, UInt16 Z)> OnAccelerometerSensibilityChanged;
        public event EventHandler<VR33BSerialPortBaudRate> OnSerialPortBaudRateChanged;
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

        private VR33BSerialPortBaudRate _SerialPortBaudRate;
        public VR33BSerialPortBaudRate SerialPortBaudRate
        {
            get
            {
                return _SerialPortBaudRate;
            }
            set
            {
                _SerialPortBaudRate = value;
                OnSerialPortBaudRateChanged?.Invoke(this, _SerialPortBaudRate);
            }
        }
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

        private (UInt16 X, UInt16 Y, UInt16 Z) _AccelerometerZero;
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerZero
        {
            get
            {
                return _AccelerometerZero;
            }
            set
            {
                _AccelerometerZero = value;
                OnAccelerometerZeroChanged?.Invoke(this, value);
            }
        }

        private (UInt16 X, UInt16 Y, UInt16 Z) _AccelerometerSensibility;
        public (UInt16 X, UInt16 Y, UInt16 Z) AccelerometerSensibility
        {
            get
            {
                return _AccelerometerSensibility;
            }
            set
            {
                _AccelerometerSensibility = value;
                OnAccelerometerSensibilityChanged?.Invoke(this, value);
            }
        }

        private int _ThresholdInPercent;
        public int ThresholdInPercent
        {
            get
            {
                return _ThresholdInPercent;
            }
            set
            {
                _ThresholdInPercent = value;
                OnThresholdInPercentChanged?.Invoke(this, value);
            }
        }

        private double _Threshold;
        public double Threshold
        {
            get
            {
                return _Threshold;
            }
            set
            {
                _Threshold = value;
                OnThresholdChanged?.Invoke(this, value);
            }
        }



    }
}
