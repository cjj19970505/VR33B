using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace VR33B
{    
    public class VR33BTerminal
    {
        public byte Address = 0x02;

        public SerialPort _SerialPort;
        public SerialPort SerialPort
        {
            set
            {
                if(_SerialPort != null)
                {
                    _SerialPort.DataReceived -= SerialPort_DataReceived;
                }
                _SerialPort = value;
                _SerialPort.DataReceived += SerialPort_DataReceived;
            }
            get
            {
                return _SerialPort;
            }
        }
        object _ReceivedBytesBufferLock;
        List<byte> _ReceivedBytesBuffer;
        DateTime _LatestSerialPortReceiveTime;

        /// <summary>
        /// 若超过这个时间时没有新的接收，则把当前的buffer全部清理掉
        /// </summary>
        TimeSpan _SerialPortBufferStayOvertimeTimeSpan;

        public VR33BTerminal()
        {
            _ReceivedBytesBuffer = new List<byte>();
            _ReceivedBytesBufferLock = new object();
            _LatestSerialPortReceiveTime = DateTime.Now;
            _SerialPortBufferStayOvertimeTimeSpan = new TimeSpan(0, 0, 1);

            SerialPort = new SerialPort("COM8");

            SerialPort.DataReceived += SerialPort_DataReceived;
            SerialPort.BaudRate = 115200;
            SerialPort.StopBits = StopBits.One;
            SerialPort.DataBits = 8;
            SerialPort.Handshake = Handshake.None;
            SerialPort.RtsEnable = true;

            SerialPort.DataReceived += SerialPort_DataReceived;
            //SerialPort.Open();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            DateTime nowDateTime = DateTime.Now;
            if(nowDateTime - _LatestSerialPortReceiveTime > _SerialPortBufferStayOvertimeTimeSpan)
            {
                _ReceivedBytesBuffer.Clear();
            }
            _LatestSerialPortReceiveTime = nowDateTime;
            var serialPort = sender as SerialPort;
            var stream = serialPort.BaseStream;
            byte[] buffer = new byte[serialPort.BytesToRead];
            stream.Read(buffer, 0, buffer.Length);
            lock(_ReceivedBytesBufferLock)
            {
                _ReceivedBytesBuffer.AddRange(buffer);
                while(_ReceivedBytesBuffer.Count>0)
                {
                    
                    int possibleMessageStartIndex = _ReceivedBytesBuffer.FindIndex(item => item == Address);
                    if(possibleMessageStartIndex < 0)
                    {
                        _ReceivedBytesBuffer.Clear();
                        break;
                    }
                    if(possibleMessageStartIndex > 0)
                    {
                        _ReceivedBytesBuffer.RemoveRange(0, possibleMessageStartIndex);
                        possibleMessageStartIndex = 0;
                    }
                    if(_ReceivedBytesBuffer.Count < 6)
                    {
                        //可能并没接受完一条消息
                        break;
                    }
                    int possibleDataLength = _ReceivedBytesBuffer[2];
                    if(_ReceivedBytesBuffer.Count < 5+possibleDataLength)
                    {
                        //可能并没接受完一条消息
                        break;
                    }
                    byte possibleCrcCodeLow = _ReceivedBytesBuffer[2 + possibleDataLength + 1];
                    byte possibleCrcCodeHigh = _ReceivedBytesBuffer[2 + possibleDataLength + 2];
                    UInt16 possibleCrcCode = (UInt16)(((0xff & possibleCrcCodeHigh) << 8) | (0xff & possibleCrcCodeLow));
                    UInt16 trueCrcCode = crc16(_ReceivedBytesBuffer.GetRange(0, 3 + possibleDataLength).ToArray());
                    if(possibleCrcCode == trueCrcCode)
                    {
                        byte[] message = _ReceivedBytesBuffer.GetRange(0, 5 + possibleDataLength).ToArray();
                        _ReceivedBytesBuffer.RemoveRange(0, 5 + possibleDataLength);
                        string s = "";
                        foreach(var m in message)
                        {
                            s += m.ToString();
                        }
                        System.Diagnostics.Debug.WriteLine(s);
                    }
                    else
                    {
                        _ReceivedBytesBuffer.RemoveAt(0);
                    }
                }
            }

            
        }

        public void Send(bool toDeviceOrGlobal, UInt16 regAddr, bool readOrNwrite, byte[] data)
        {
            List<byte> sendBytesList = new List<byte>();
            if(toDeviceOrGlobal)
            {
                sendBytesList.Add(Address);
            }
            else
            {
                sendBytesList.Add(0xff);
            }
            
            sendBytesList.Add(readOrNwrite ? (byte)0x03 : (byte)0x06);
            sendBytesList.Add(BitConverter.GetBytes(regAddr)[1]);
            sendBytesList.Add(BitConverter.GetBytes(regAddr)[0]);
            sendBytesList.AddRange(data);
            UInt16 crc = crc16(sendBytesList.ToArray());
            sendBytesList.AddRange(BitConverter.GetBytes(crc));
            byte[] sendBytes = sendBytesList.ToArray();
            SerialPort.Write(sendBytes, 0, sendBytes.Length);

        }


        UInt16 crc16(byte[] buf)
        {
            UInt16 i, j, crc;
            crc = 0xffff;
            int length = buf.Length;
            for (i = 0; i < length; i++)
            {
                crc ^= (UInt16)buf[i]; //°´Î»È¡·´
                for (j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xa001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 若没有收到回复，则间隔多长时间重新发一次
        /// </summary>
        TimeSpan RepeatTimeSpanWhenNoResponse { get; }
        /// <summary>
        /// 最多发送多少次，超过这个次数宣告失败
        /// </summary>
        int MaximumRepeatCount { get; }
        

    }

    public struct VR33BSendData
    {
        public enum MessageType { Read = 0x03, Write = 0x06};
        public byte DeviceAddress { get; set; }
        public MessageType ReadOrWrite { get; set; }
        public UInt16 RegisterAddress { get; set; }
        public byte[] Data { get; set; }
    }

    
}
