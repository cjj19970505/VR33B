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

        object _CommandSessionQueueLock;
        Queue<CommandSession> _CommandSessionQueue;

        /// <summary>
        /// 若超过这个时间时没有新的接收，则把当前的buffer全部清理掉
        /// </summary>
        TimeSpan _SerialPortReceiveBufferStayOvertimeTimeSpan;

        public event EventHandler<VR33BReceiveData> OnReceived;

        public VR33BTerminal()
        {
            _ReceivedBytesBuffer = new List<byte>();
            _ReceivedBytesBufferLock = new object();
            _LatestSerialPortReceiveTime = DateTime.Now;
            _SerialPortReceiveBufferStayOvertimeTimeSpan = new TimeSpan(0, 0, 1);

            _CommandSessionQueueLock = new object();
            _CommandSessionQueue = new Queue<CommandSession>();
            _SendCommandToSerialPortFromQueueTask();
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
            if(nowDateTime - _LatestSerialPortReceiveTime > _SerialPortReceiveBufferStayOvertimeTimeSpan)
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
                    UInt16 trueCrcCode = VR33BUtility.Crc16(_ReceivedBytesBuffer.GetRange(0, 3 + possibleDataLength).ToArray());
                    if(possibleCrcCode == trueCrcCode)
                    {
                        byte[] message = _ReceivedBytesBuffer.GetRange(0, 5 + possibleDataLength).ToArray();
                        _ReceivedBytesBuffer.RemoveRange(0, 5 + possibleDataLength);
                        

                        OnReceived?.Invoke(this, VR33BReceiveData.FromByteArray(message));
                    }
                    else
                    {
                        _ReceivedBytesBuffer.RemoveAt(0);
                    }
                }
            }

            
        }
        public void Send(VR33BSendData sendData)
        {
            SerialPort.Write(sendData.SendBytes, 0, sendData.SendBytes.Length);
        }

        public void SendCommand(ICommand command)
        {
            
        }

        public Task<(bool Success, VR33BReceiveData Response)> SendCommandAsync(ICommand command)
        {
            CommandSession session = new CommandSession(command);
            lock (_CommandSessionQueueLock)
            {
                _CommandSessionQueue.Enqueue(session);
            }
            return Task.Run(() =>
            {
                while(session.CommandState == VR33BCommandState.Sending || session.CommandState == VR33BCommandState.Idle)
                {
                    
                }

                if(session.CommandState == VR33BCommandState.Success)
                {
                    return (true, session.Response);
                }
                return (false, session.Response);
            });
        }

        public Task _SendCommandToSerialPortFromQueueTask()
        {
            return Task.Run(() =>
            {
                DateTime sessionStartTime = DateTime.Now;
                int sessionRepeatCount = 0;
                DateTime singleRepeatStartTime = sessionStartTime;

                CommandSession CurrentSession = null;
                EventHandler<VR33BReceiveData> onSerialReceive = (object sender, VR33BReceiveData e) =>
                {
                    if(CurrentSession == null)
                    {
                        return;
                    }
                    if(CurrentSession.CommandState == VR33BCommandState.Sending)
                    {
                        bool isResponse = CurrentSession.Command.IsResponse(e);
                        if(isResponse)
                        {
                            CurrentSession.Response = e;
                            CurrentSession.CommandState = VR33BCommandState.Success;
                        }
                    }
                };

                OnReceived += onSerialReceive;
                

                while (true)
                {
                    lock(_CommandSessionQueueLock)
                    {
                        if(_CommandSessionQueue.Count == 0 && (CurrentSession == null || CurrentSession.CommandState == VR33BCommandState.Success || CurrentSession.CommandState == VR33BCommandState.Failed))
                        {
                            continue;
                        }
                        else if(_CommandSessionQueue.Count != 0)
                        {
                            CurrentSession = _CommandSessionQueue.Dequeue();
                        }
                    }
                    if(CurrentSession.CommandState == VR33BCommandState.Idle)
                    {
                        sessionStartTime = singleRepeatStartTime = DateTime.Now;
                        CurrentSession.CommandState = VR33BCommandState.Sending;
                        Send(CurrentSession.Command.SendData);
                        
                    }
                    else if(CurrentSession.CommandState == VR33BCommandState.Sending)
                    {
                        DateTime now = DateTime.Now;
                        if(now - singleRepeatStartTime >= CurrentSession.Command.RepeatTimeSpanWhenNoResponse)
                        {
                            if(sessionRepeatCount >= CurrentSession.Command.MaximumRepeatCount)
                            {
                                CurrentSession.CommandState = VR33BCommandState.Failed;
                            }
                            else
                            {
                                singleRepeatStartTime = now;
                                Send(CurrentSession.Command.SendData);
                            }
                        }
                    }
                }
            });
        }
    }

    public enum VR33BCommandState { Idle, Sending, Success, Failed}

    /// <summary>
    /// 这货是这么工作的
    /// 当串口接收到消息后，会交给当前发送命令的IsResponse进行检查，如果是这条消息的回复的话就比较ok，如果不是的话
    /// T是返回消息的类型
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 若没有收到回复，则间隔多长时间重新发一次
        /// </summary>
        TimeSpan RepeatTimeSpanWhenNoResponse { get; }
        /// <summary>
        /// 最多发送多少次，超过这个次数宣告失败
        /// 如果为0，那就是不用重复发送
        /// </summary>
        int MaximumRepeatCount { get; }

        VR33BSendData SendData { get; }

        bool IsResponse(VR33BReceiveData receiveData);
    }

    internal class CommandSession
    {
        public ICommand Command { get; set; }
        public VR33BCommandState CommandState { get; set; }

        public VR33BReceiveData Response { get; set; }
        
        public CommandSession(ICommand command)
        {
            Command = command;
            CommandState = VR33BCommandState.Idle;
        }

        

    }
    public enum VR33BMessageType { Read = 0x03, Write = 0x06 };
    public struct VR33BSendData
    {
        public byte DeviceAddress { get; set; }
        public VR33BMessageType ReadOrWrite { get; set; }
        public UInt16 RegisterAddress { get; set; }
        public byte[] Data { get; set; }

        /// <summary>
        /// Final Send
        /// </summary>
        public byte[] SendBytes
        {
            get
            {
                /*
                List<byte> sendBytesList = new List<byte>();
                if (toDeviceOrGlobal)
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
                UInt16 crc = VR33BUtility.Crc16(sendBytesList.ToArray());
                sendBytesList.AddRange(BitConverter.GetBytes(crc));
                byte[] sendBytes = sendBytesList.ToArray();
                SerialPort.Write(sendBytes, 0, sendBytes.Length);
                */
                List<byte> sendBytesList = new List<byte>
                {
                    DeviceAddress,
                    (Byte)ReadOrWrite,
                    BitConverter.GetBytes(RegisterAddress)[1],
                    BitConverter.GetBytes(RegisterAddress)[0]
                };
                sendBytesList.AddRange(Data);
                UInt16 crc = VR33BUtility.Crc16(sendBytesList.ToArray());
                sendBytesList.AddRange(BitConverter.GetBytes(crc));
                return sendBytesList.ToArray();

            }
        }
    }

    public struct VR33BReceiveData
    {
        public byte DeviceAddress { get; set; }
        public VR33BMessageType ReadOrWrite { get; set; }
        public byte[] Data { get; set; }

        public static VR33BReceiveData FromByteArray(byte[] byteArray)
        {
            VR33BReceiveData receiveData = new VR33BReceiveData();
            receiveData.DeviceAddress = byteArray[0];
            receiveData.ReadOrWrite = (VR33BMessageType)byteArray[1];
            receiveData.Data = new List<byte>(byteArray).GetRange(3, (int)(byteArray[2])).ToArray();
            return receiveData;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[DeviceAddress:{0:x2} |", DeviceAddress);
            sb.AppendFormat("ReadOrWrite:{0:x2} |", (byte)ReadOrWrite);
            sb.Append("Data:");
            
            foreach(var b in Data)
            {
                sb.AppendFormat("{0:x2} ", b);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }

    public static class VR33BUtility
    {
        internal static UInt16 Crc16(byte[] buf)
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


    public class ReadAddressCommand : ICommand
    {
        private VR33BSendData _SendData;
        public TimeSpan RepeatTimeSpanWhenNoResponse
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 50);
            }
        }

        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }

        public VR33BSendData SendData
        {
            get
            {
                return _SendData;
            }
        }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            if(receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 1)
            {
                return true;
            }
            return false;
        }

        public ReadAddressCommand()
        {
            _SendData = new VR33BSendData
            {
                DeviceAddress = 0xff,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x01,
                Data = new byte[] { 0, 1 }
            };
        }
    }

    public class ReadAccelerometerRange : ICommand
    {
        private VR33BSendData _SendData;
        public TimeSpan RepeatTimeSpanWhenNoResponse
        {
            get
            {
                return new TimeSpan(0, 0, 0, 0, 50);
            }
        }

        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }

        public VR33BSendData SendData
        {
            get
            {
                return _SendData;
            }
        }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 1)
            {
                return true;
            }
            return false;
        }

        public ReadAccelerometerRange(VR33BTerminal vr33bTerminal)
        {
            _SendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.Address,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0016,
                Data = new byte[] { 0, 0 }
            };
        }
    }

}
