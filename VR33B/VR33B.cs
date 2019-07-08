using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace VR33B
{
    public enum VR33BSettingResult { Succss, Falied}
    public enum VR33BReadResult { Success, Failed}

    public enum VR33BConnectionState { NotConnected, Connecting, Success, Failed}

    public enum VR33BState { Idle, Setting, Reading}
    public class VR33BTerminal
    {
        public const string SampleNamePrefix = "Sample_";
        //public byte Address = 0xff;
        private SerialPort _SerialPort;
        public SerialPort SerialPort
        {
            set
            {
                if (_SerialPort != null)
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
        public event EventHandler<VR33BSendData> OnSerialPortSent;

        public event EventHandler<VR33BSampleProcess> OnVR33BSampleStarted;
        public event EventHandler<VR33BSampleValue> OnVR33BSampleValueReceived;
        public event EventHandler OnVR33BSampleEnded;

        public event EventHandler<VR33BConnectionState> OnConnectonStateChanged;
        public event EventHandler<byte[]> SerialPortRawDataReceived;

        public IVR33BStorage VR33BSampleDataStorage { get; }

        public bool UseFakeSampleValueGenerator { get; }
        /// <summary>
        /// 是否采样中
        /// </summary>
        public bool Sampling { get; private set; }
        private long _CurrentSampleIndex = 0;
        public VR33BSampleProcess _CurrentSampleProcess;

        /// <summary>
        /// PC中保存的最新的设置（可能和实际有出入，但是一旦获取到新的设置后会自动填充进这个设置里，（除非前端作死自己去使用SendCommand函数啥的）
        /// </summary>
        public VR33BSetting LatestSetting { get; internal set; }

        public VR33BConnectionState ConnectionState { get; private set; }

        private Task _SendCommandToSerialPortFromQueueTask;
        private object _SendCommandToSerialPortFromQueueTaskLock = new object();

        public VR33BTerminal(IVR33BStorage storage, bool useFakeSampleValueGenerator = false)
        {
            LatestSetting = new VR33BSetting();
            LatestSetting.DeviceAddress = 0xff;

            _ReceivedBytesBuffer = new List<byte>();
            _ReceivedBytesBufferLock = new object();
            _LatestSerialPortReceiveTime = DateTime.Now;
            _SerialPortReceiveBufferStayOvertimeTimeSpan = new TimeSpan(0, 0, 1);

            _CommandSessionQueueLock = new object();
            _CommandSessionQueue = new Queue<CommandSession>();
            //_RunSendCommandToSerialPortFromQueueTask();
            SerialPort = new SerialPort();
            if(SerialPort.GetPortNames().Length > 0)
            {
                SerialPort.PortName = SerialPort.GetPortNames()[0];
            }
            SerialPort.DataReceived += SerialPort_DataReceived;
            SerialPort.BaudRate = 115200;
            SerialPort.StopBits = StopBits.One;
            SerialPort.DataBits = 8;
            SerialPort.Handshake = Handshake.None;
            SerialPort.RtsEnable = true;

            ConnectionState = VR33BConnectionState.NotConnected;

            _CurrentSampleIndex = 0;

            SerialPort.DataReceived += SerialPort_DataReceived;
            UseFakeSampleValueGenerator = useFakeSampleValueGenerator;
            if(UseFakeSampleValueGenerator)
            {
                _FakeDataGenerateTask();
            }
            storage.VR33BTerminal = this;
            VR33BSampleDataStorage = storage;
            //VR33BSampleDataStorage = new VR33BSampleDataStorage(this);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            DateTime nowDateTime = DateTime.Now;
            if (nowDateTime - _LatestSerialPortReceiveTime > _SerialPortReceiveBufferStayOvertimeTimeSpan)
            {
                _ReceivedBytesBuffer.Clear();
            }
            _LatestSerialPortReceiveTime = nowDateTime;
            var serialPort = sender as SerialPort;
            var stream = serialPort.BaseStream;
            byte[] buffer = new byte[serialPort.BytesToRead];
            stream.Read(buffer, 0, buffer.Length);
            SerialPortRawDataReceived?.Invoke(this, buffer);
            lock (_ReceivedBytesBufferLock)
            {
                _ReceivedBytesBuffer.AddRange(buffer);
                while (_ReceivedBytesBuffer.Count > 0)
                {

                    int possibleMessageStartIndex = _ReceivedBytesBuffer.FindIndex(item => item == LatestSetting.DeviceAddress);
                    if (possibleMessageStartIndex < 0)
                    {
                        _ReceivedBytesBuffer.Clear();
                        break;
                    }
                    if (possibleMessageStartIndex > 0)
                    {
                        _ReceivedBytesBuffer.RemoveRange(0, possibleMessageStartIndex);
                        possibleMessageStartIndex = 0;
                    }
                    if (_ReceivedBytesBuffer.Count < 6)
                    {
                        //可能并没接受完一条消息
                        break;
                    }
                    int possibleDataLength = _ReceivedBytesBuffer[2];
                    if (_ReceivedBytesBuffer.Count < 5 + possibleDataLength)
                    {
                        //可能并没接受完一条消息
                        break;
                    }
                    byte possibleCrcCodeLow = _ReceivedBytesBuffer[2 + possibleDataLength + 1];
                    byte possibleCrcCodeHigh = _ReceivedBytesBuffer[2 + possibleDataLength + 2];
                    UInt16 possibleCrcCode = (UInt16)(((0xff & possibleCrcCodeHigh) << 8) | (0xff & possibleCrcCodeLow));
                    UInt16 trueCrcCode = VR33BUtility.Crc16(_ReceivedBytesBuffer.GetRange(0, 3 + possibleDataLength).ToArray());
                    if (possibleCrcCode == trueCrcCode)
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
            if(!SerialPort.IsOpen)
            {
                return;
            }
            SerialPort.Write(sendData.SendBytes, 0, sendData.SendBytes.Length);
            OnSerialPortSent?.Invoke(this, sendData);
        }

        public Task<(bool Success, VR33BReceiveData Response, CommandSession CommandSession)> SendCommandAsync(ICommand command)
        {
            CommandSession session = new CommandSession(command);
            lock (_CommandSessionQueueLock)
            {
                _CommandSessionQueue.Enqueue(session);
            }
            bool shouldTurnOnTask = false;
            lock(_SendCommandToSerialPortFromQueueTaskLock)
            {
                if(_SendCommandToSerialPortFromQueueTask == null)
                {
                    shouldTurnOnTask = true;
                }
            }
            if(shouldTurnOnTask)
            {
                _RunSendCommandToSerialPortFromQueueTask();
            }
            return Task.Run(() =>
            {
                while (session.CommandState == VR33BCommandState.Sending || session.CommandState == VR33BCommandState.Idle)
                {
                    
                }

                if (session.CommandState == VR33BCommandState.Success)
                {
                    return (true, session.Response, session);
                }
                else if(session.CommandState == VR33BCommandState.Failed)
                {
                    if(ConnectionState == VR33BConnectionState.Success)
                    {
                        ConnectionState = VR33BConnectionState.Failed;
                        SerialPort.Close();
                        OnConnectonStateChanged?.Invoke(this, ConnectionState);
                    }
                }
                return (false, session.Response, session);
            });
        }

        public Task _RunSendCommandToSerialPortFromQueueTask()
        {
            return Task.Run(() =>
            {
                DateTime sessionStartTime = DateTime.Now;
                int sessionRepeatCount = 0;
                DateTime singleCommandStartTime = sessionStartTime;

                CommandSession CurrentSession = null;
                EventHandler<VR33BReceiveData> onSerialReceive = (object sender, VR33BReceiveData e) =>
                {
                    if (CurrentSession == null)
                    {
                        return;
                    }
                    if (CurrentSession.CommandState == VR33BCommandState.Sending)
                    {
                        bool isResponse = CurrentSession.Command.IsResponse(e);
                        if (isResponse)
                        {
                            CurrentSession.Response = e;
                            CurrentSession.CommandState = VR33BCommandState.Success;
                        }
                    }
                };

                OnReceived += onSerialReceive;


                while (true)
                {
                    
                    lock (_CommandSessionQueueLock)
                    {
                        lock(_SendCommandToSerialPortFromQueueTaskLock)
                        {
                            if (_CommandSessionQueue.Count == 0 && (CurrentSession == null || CurrentSession.CommandState == VR33BCommandState.Success || CurrentSession.CommandState == VR33BCommandState.Failed))
                            {
                                _SendCommandToSerialPortFromQueueTask = null;
                                OnReceived -= onSerialReceive;
                                break;
                            }
                        }
                        if (_CommandSessionQueue.Count == 0 && (CurrentSession == null || CurrentSession.CommandState == VR33BCommandState.Success || CurrentSession.CommandState == VR33BCommandState.Failed))
                        {
                            continue;
                        }
                        else if (_CommandSessionQueue.Count != 0)
                        {
                            CurrentSession = _CommandSessionQueue.Dequeue();
                        }
                    }
                    if (CurrentSession.CommandState == VR33BCommandState.Idle)
                    {
                        sessionRepeatCount = 0;
                        sessionStartTime = singleCommandStartTime = DateTime.Now;
                        CurrentSession.CommandState = VR33BCommandState.Sending;
                        Send(CurrentSession.Command.SendDataSequence[0].SendData);

                    }
                    else if (CurrentSession.CommandState == VR33BCommandState.Sending)
                    {
                        DateTime now = DateTime.Now;


                        if (now - singleCommandStartTime >= CurrentSession.CurrentSendDataAndInterval.IntervalTimeSpan)
                        {
                            if (CurrentSession.CurrentSendingDataIndex == CurrentSession.Command.SendDataSequence.Length - 1)
                            {
                                sessionRepeatCount++;
                                CurrentSession.CurrentSendingDataIndex = 0;
                            }
                            else
                            {
                                CurrentSession.CurrentSendingDataIndex++;
                            }
                            if (sessionRepeatCount > CurrentSession.Command.MaximumRepeatCount)
                            {
                                if(CurrentSession.Command.OnTimeout())
                                {
                                    CurrentSession.CommandState = VR33BCommandState.Success;
                                }
                                else
                                {
                                    CurrentSession.CommandState = VR33BCommandState.Failed;
                                }
                            }
                            else
                            {
                                Send(CurrentSession.CurrentSendDataAndInterval.SendData);
                            }
                            singleCommandStartTime = now;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// return True if isSampling or successfully start sample, false if otherwise
        /// </summary>
        /// <returns>True if isSampling or successfully start sample, false if otherwise</returns>
        public async Task<bool> StartSampleAsync()
        {
            _CurrentSampleIndex = 0;
            if (UseFakeSampleValueGenerator)
            {
                if (Sampling)
                {
                    return true;
                }
                Sampling = true;
                _CurrentSampleProcess = new VR33BSampleProcess
                {
                    Name = "Sample",
                    Guid = Guid.NewGuid()
                };
                OnVR33BSampleStarted?.Invoke(this, _CurrentSampleProcess);
                return true;
            }

            if (Sampling)
            {
                return true;
            }
            var response = await SendCommandAsync(new StartSampleCommand(this));
            if (response.Success)
            {
                Sampling = true;
                _CurrentSampleProcess = new VR33BSampleProcess
                {
                    Name = "Sample",
                    Guid = Guid.NewGuid()
                };
                OnVR33BSampleStarted?.Invoke(this, _CurrentSampleProcess);
                OnVR33BSampleValueReceived?.Invoke(this, VR33BSampleValue.FromVR33BReceiveData(response.Response, LatestSetting, 0, _CurrentSampleProcess));
                _CurrentSampleIndex = 1;
                this.OnReceived += VR33BTerminal_OnReceived;
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> StopSampleAsync()
        {
            if(UseFakeSampleValueGenerator)
            {
                Sampling = false;
                OnVR33BSampleEnded?.Invoke(this, null);
                return true;
            }

            var response = await SendCommandAsync(new StopSampleCommand(this));
            if(response.Success)
            {
                Sampling = false;
                OnVR33BSampleEnded?.Invoke(this, null);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void VR33BTerminal_OnReceived(object sender, VR33BReceiveData e)
        {
            //Check if is samplevalue data
            if(Sampling)
            {
                if(e.ReadOrWrite == VR33BMessageType.Read && e.Data.Length == 17)
                {

                    VR33BSampleValue sampleValue = VR33BSampleValue.FromVR33BReceiveData(e, LatestSetting, _CurrentSampleIndex, _CurrentSampleProcess);
                    _CurrentSampleIndex++;
                    OnVR33BSampleValueReceived?.Invoke(this, sampleValue);
                }
            }
        }

        public async Task<(VR33BReadResult, VR33BSampleFrequence)> ReadSampleFrequencyAsync()
        {
            var response = await SendCommandAsync(new ReadSampleFrequencyCommand(this));
            if (response.Success)
            {
                VR33BSampleFrequence sampleFrequency = (VR33BSampleFrequence)BitConverter.ToInt16(new byte[] { response.Response.Data[1], response.Response.Data[0] }, 0);
                LatestSetting.SampleFrequence = sampleFrequency;
                return (VR33BReadResult.Success, sampleFrequency);
            }
            else
            {
                return (VR33BReadResult.Failed, VR33BSampleFrequence._1Hz);
            }
        }

        public async Task<(VR33BReadResult, VR33BAccelerometerRange)> ReadAccelerometerRangeAsync()
        {
            var response = await SendCommandAsync(new ReadAccelerometerRange(this));
            if (response.Success)
            {
                VR33BAccelerometerRange accelerometerRange = (VR33BAccelerometerRange)response.Response.Data[0];
                LatestSetting.AccelerometerRange = accelerometerRange;
                return (VR33BReadResult.Success, accelerometerRange);
            }
            else
            {
                return (VR33BReadResult.Failed, VR33BAccelerometerRange._2g);
            }
        }

        public async Task<(VR33BReadResult, byte)> ReadDeviceAddressAsync()
        {
            LatestSetting.DeviceAddress = 0xff;
            var response = await SendCommandAsync(new ReadAddressCommand());
            if(response.Success)
            {
                byte address = response.Response.Data[0];
                LatestSetting.DeviceAddress = address;
                return (VR33BReadResult.Success, response.Response.Data[0]);
            }
            else
            {
                return (VR33BReadResult.Failed, 0);
            }
        }
        public async Task<VR33BSettingResult> SetSampleFrequencyAsync(VR33BSampleFrequence sampleFrequency)
        {
            var response = await SendCommandAsync(new SetSampleFrequencyCommand(this, sampleFrequency));
            if(response.Success)
            {
                LatestSetting.SampleFrequence = sampleFrequency;
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }
        public async Task<VR33BSettingResult> SetAccelerometerRangeAsync(VR33BAccelerometerRange accelerometerRange)
        {
            var response = await SendCommandAsync(new SetAccelerometerRangeCommand(this, accelerometerRange));
            if (response.Success)
            {
                LatestSetting.AccelerometerRange = accelerometerRange;
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }

        public async Task<VR33BReadResult> ReadAllSettingAsync()
        {
            var response = await SendCommandAsync(new ReadAllSettingCommand(this));
            try
            {
                var data = response.Response.Data;
                var baudRate = (VR33BSerialPortBaudRate)data[0];
                //var checksum = (CheckSo)
                var workingMode = (VR33BWorkingMode)data[3];
                int version = data[4] * 10 + data[5];
                var frequency = (VR33BSampleFrequence)BitConverter.ToInt16(new byte[] { data[7], data[6] }, 0);
                var range = (VR33BAccelerometerRange)data[8];
                var xSensibility = BitConverter.ToUInt16(new byte[] { data[10], data[9] }, 0);
                var xZero = BitConverter.ToUInt16(new byte[] { data[12], data[11] }, 0);
                var ySensibility = BitConverter.ToUInt16(new byte[] { data[14], data[13] }, 0);
                var yZero = BitConverter.ToUInt16(new byte[] { data[16], data[15] }, 0);
                var zSensibility = BitConverter.ToUInt16(new byte[] { data[18], data[17] }, 0);
                var zZero = BitConverter.ToUInt16(new byte[] { data[20], data[19] }, 0);
                LatestSetting.AccelerometerSensibility = (xSensibility, ySensibility, zSensibility);
                LatestSetting.AccelerometerZero = (xZero, yZero, zZero);

                int thresholdInPercent = data[21];
                double threshold = data[22] / 10.0;


                LatestSetting.SampleFrequence = frequency;
                LatestSetting.AccelerometerRange = range;
                LatestSetting.ThresholdInPercent = thresholdInPercent;
                LatestSetting.Threshold = threshold;
                LatestSetting.SerialPortBaudRate = baudRate;


                if (response.Success)
                {
                    return VR33BReadResult.Success;
                }
                else
                {
                    return VR33BReadResult.Failed;
                }
            }
            catch(Exception e)
            {
                return VR33BReadResult.Failed;
            }
            
        }

        public async Task<VR33BSettingResult> CalibrateXAsync()
        {
            var response = await SendCommandAsync(new CalibrateXCommand(this));
            if (response.Success)
            {
                /*
                var data = response.Response.Data;
                UInt16 sensitiveX = BitConverter.ToUInt16(new byte[] { data[1], data[0] }, 0);
                UInt16 zeroX = BitConverter.ToUInt16(new byte[] { data[3], data[2] }, 0);
                var sensitive = LatestSetting.AccelerometerSensibility;
                var zero = LatestSetting.AccelerometerZero;
                sensitive.X = sensitiveX;
                zero.X = zeroX;
                LatestSetting.AccelerometerSensibility = sensitive;
                LatestSetting.AccelerometerZero = zero;
                */
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }

        public async Task<VR33BSettingResult> CalibrateYAsync()
        {
            var response = await SendCommandAsync(new CalibrateYCommand(this));
            if (response.Success)
            {
                var data = response.Response.Data;
                UInt16 sensitiveY = BitConverter.ToUInt16(new byte[] { data[1], data[0] }, 0);
                UInt16 zeroY = BitConverter.ToUInt16(new byte[] { data[3], data[2] }, 0);
                var sensitive = LatestSetting.AccelerometerSensibility;
                var zero = LatestSetting.AccelerometerZero;
                sensitive.Y = sensitiveY;
                zero.Y = zeroY;
                LatestSetting.AccelerometerSensibility = sensitive;
                LatestSetting.AccelerometerZero = zero;
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }

        public async Task<VR33BSettingResult> CalibrateZAsync()
        {
            var response = await SendCommandAsync(new CalibrateZCommand(this));
            if (response.Success)
            {
                var data = response.Response.Data;
                UInt16 sensitiveZ = BitConverter.ToUInt16(new byte[] { data[1], data[0] }, 0);
                UInt16 zeroZ = BitConverter.ToUInt16(new byte[] { data[3], data[2] }, 0);
                var sensitive = LatestSetting.AccelerometerSensibility;
                var zero = LatestSetting.AccelerometerZero;
                sensitive.Z = sensitiveZ;
                zero.Z = zeroZ;
                LatestSetting.AccelerometerSensibility = sensitive;
                LatestSetting.AccelerometerZero = zero;
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }

        private Task _FakeDataGenerateTask()
        {
            return Task.Run(() =>
            {
                double frequency = 200;
                DateTime _LatestSampleDateTime = DateTime.Now;
                long sampleIndex = 0;

                OnVR33BSampleStarted += (object sender, VR33BSampleProcess e) => { sampleIndex = 0; };
                while (true)
                {
                    if (Sampling)
                    {
                        DateTime now = DateTime.Now;
                        if((now - _LatestSampleDateTime).TotalSeconds > 1.0/frequency)
                        {
                            _LatestSampleDateTime = now;
                            var sampleValue = _FakeDataGenerateFunc(now, sampleIndex);
                            sampleIndex++;
                            OnVR33BSampleValueReceived?.Invoke(this, sampleValue);
                        }
                    }
                    
                }
                
            });
        }

        private VR33BSampleValue _FakeDataGenerateFunc(DateTime dateTime, long sampleIndex)
        {
            DateTime initDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
            return new VR33BSampleValue
            {
                SampleIndex = sampleIndex,
                SampleDateTime = dateTime,
                RawAccelerometerValue = ((UInt16)(0.5*Math.Sin(0.05*(dateTime - initDateTime).TotalMilliseconds) * Int16.MaxValue), (UInt16)(0.2*Math.Cos(0.001*(dateTime - initDateTime).TotalMilliseconds) * Int16.MaxValue), (UInt16)(0.1*Math.Sin(0.04*(dateTime - initDateTime).TotalMilliseconds) * Int16.MaxValue)),
                RawTemperature = 0,
                RawHumidity = 3
            };
        }

        public async Task ConnectAsync()
        {
            if(ConnectionState == VR33BConnectionState.Success)
            {
                return;
            }
            LatestSetting.DeviceAddress = 0xff;
            ConnectionState = VR33BConnectionState.Connecting;
            OnConnectonStateChanged?.Invoke(this, ConnectionState);
            if(SerialPort.IsOpen)
            {
                ConnectionState = VR33BConnectionState.Failed;
                OnConnectonStateChanged?.Invoke(this, ConnectionState);
                return;
            }
            try
            {
                lock (_ReceivedBytesBufferLock)
                {
                    _ReceivedBytesBuffer.Clear();
                }
                lock (_CommandSessionQueueLock)
                {
                    _CommandSessionQueue.Clear();
                }
                SerialPort.Open();
                await Task.Run(() =>
                {
                    Thread.Sleep(500);
                });
                
                var addresssResult = await ReadDeviceAddressAsync();
                if(addresssResult.Item1 == VR33BReadResult.Failed)
                {
                    ConnectionState = VR33BConnectionState.Failed;
                    OnConnectonStateChanged?.Invoke(this, ConnectionState);
                    SerialPort.Close();
                    return;
                }
                else
                {
                    //var readAcceRangeResult = (await ReadAccelerometerRangeAsync()).Item1;
                    //var readSampleFrequencyRangeResult = (await ReadSampleFrequencyAsync()).Item1;
                    var readAllSettingResult = await ReadAllSettingAsync();
                    
                    if(readAllSettingResult == VR33BReadResult.Success)
                    {
                        ConnectionState = VR33BConnectionState.Success;
                        OnConnectonStateChanged?.Invoke(this, ConnectionState);
                        return;
                    }
                    else
                    {
                        ConnectionState = VR33BConnectionState.Failed;
                        OnConnectonStateChanged?.Invoke(this, ConnectionState);
                        SerialPort.Close();
                        return;
                    }
                    
                }
            }
            catch(Exception e)
            {
                if(SerialPort.IsOpen)
                {
                    SerialPort.Close();
                }
                ConnectionState = VR33BConnectionState.Failed;
                OnConnectonStateChanged?.Invoke(this, ConnectionState);
                throw e;
            }
            
        }

        public async Task<VR33BSettingResult> SetThresholdInPercent(int thresholdInPercent)
        {
            var response = await SendCommandAsync(new SetThresholdInPercentCommand(this, thresholdInPercent));
            if(response.Success)
            {
                int receivedThresholdInPercent = response.Response.Data[0];
                double receivedThreshold = response.Response.Data[1]/10.0;
                LatestSetting.Threshold = receivedThreshold;
                LatestSetting.ThresholdInPercent = receivedThresholdInPercent;
                return VR33BSettingResult.Succss;
            }
            else
            {
                return VR33BSettingResult.Falied;
            }
        }

        public async Task<VR33BSettingResult> ResetAllSetting()
        {
            var command = new ResetCommand(this);
            LatestSetting.DeviceAddress = 0x01;
            var response = await SendCommandAsync(command);
            if(response.Success)
            {
                var readResult = await ReadAllSettingAsync();
                if(readResult == VR33BReadResult.Success)
                {
                    return VR33BSettingResult.Succss;
                }
            }
            return VR33BSettingResult.Falied;
        }

        
        public async Task<VR33BSettingResult> SetDeviceAddressAsync(byte newAddress)
        {
            var command = new SetAddressCommand(this, newAddress);
            LatestSetting.DeviceAddress = 0xff;
            var response = await SendCommandAsync(command);
            if(response.Success)
            {
                LatestSetting.DeviceAddress = response.Response.Data[0];
                return VR33BSettingResult.Succss;
            }
            return VR33BSettingResult.Falied;
        }
        

        
        
    }

    public enum VR33BCommandState { Idle, Sending, Success, Failed }

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
        //TimeSpan RepeatTimeSpanWhenNoResponse { get; }

        /// <summary>
        /// 最多发送多少次，超过这个次数宣告失败
        /// 如果为0，那就是不用重复发送
        /// </summary>
        int MaximumRepeatCount { get; }

        /// <summary>
        /// IntervalTimeSpan是当前指令和发送下一条指令的时间间隔
        /// 最后一条指令的IntervalTimeSpan就是最后一条指令和下一次重复的指令的间隔
        /// </summary>
        (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence { get; }

        bool IsResponse(VR33BReceiveData receiveData);
        bool OnTimeout();
    }

    public class CommandSession
    {
        public ICommand Command { get; set; }
        public VR33BCommandState CommandState { get; internal set; }

        internal int CurrentSendingDataIndex { get; set; }
        internal (VR33BSendData SendData, TimeSpan IntervalTimeSpan) CurrentSendDataAndInterval
        {
            get
            {
                return Command.SendDataSequence[CurrentSendingDataIndex];
            }
        }

        public VR33BReceiveData Response { get; internal set; }

        internal CommandSession(ICommand command)
        {
            Command = command;
            CommandState = VR33BCommandState.Idle;
            CurrentSendingDataIndex = 0;
            Response = new VR33BReceiveData();
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[DeviceAddress:{0:x2} |", DeviceAddress);
            sb.AppendFormat("ReadOrWrite:{0:x2} |", (byte)ReadOrWrite);
            sb.AppendFormat("RegisterAddress:{0:x2} |", RegisterAddress);
            sb.Append("Data:");

            foreach (var b in Data)
            {
                sb.AppendFormat("{0:x2} ", b);
            }
            sb.Append("]");
            return sb.ToString();
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

        public byte[] RawByteArray
        {
            get
            {
                List<byte> byteArrayList = new List<byte>();
                byteArrayList.Add(DeviceAddress);
                byteArrayList.Add((byte)ReadOrWrite);
                byteArrayList.Add((byte)Data.Length);
                for(int i = 0; i<Data.Length;i++)
                {
                    byteArrayList.Add(Data[i]);
                }
                var crcResult = VR33BUtility.Crc16(byteArrayList.ToArray());
                byteArrayList.AddRange(BitConverter.GetBytes( crcResult));
                return byteArrayList.ToArray();

            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[DeviceAddress:{0:x2} |", DeviceAddress);
            sb.AppendFormat("ReadOrWrite:{0:x2} |", (byte)ReadOrWrite);
            sb.Append("Data:");

            foreach (var b in Data)
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
        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }


        private (VR33BSendData, TimeSpan)[] _SendDataSequence;
        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence
        {
            get
            {
                return _SendDataSequence;
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

        public bool OnTimeout()
        {
            return false;
        }

        public ReadAddressCommand()
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = 0xff,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x01,
                Data = new byte[] { 0, 1 }
            };
            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 500)) };
        }
    }

    public class ReadAccelerometerRange : ICommand
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
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 1)
            {
                return true;
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public ReadAccelerometerRange(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0016,
                Data = new byte[] { 0, 0 }
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 50)) };
        }
    }

    public class ReadSampleFrequencyCommand : ICommand
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
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 2)
            {
                return true;
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public ReadSampleFrequencyCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0017,
                Data = new byte[] { 0, 0 }
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 50)) };
        }
    }

    public class SetAccelerometerRangeCommand : ICommand
    {
        public VR33BAccelerometerRange Range { get; set; }
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
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 1)
            {
                if(receiveData.Data[0] == (byte)Range)
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

        public SetAccelerometerRangeCommand(VR33BTerminal vr33bTerminal, VR33BAccelerometerRange range)
        {
            Range = range;
            var setData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0016,
                Data = new byte[] { BitConverter.GetBytes((UInt16)Range)[1], BitConverter.GetBytes((UInt16)Range)[0] }
            };
            var readData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0016,
                Data = new byte[] { 0, 0 }
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[]
            {
                (setData, new TimeSpan(0, 0, 0, 0, 100)),
                (readData, new TimeSpan(0, 0, 0, 0, 50))
            };
        }
    }

    public class StartSampleCommand : ICommand
    {
        public int MaximumRepeatCount
        {
            get
            {
                return 10;
            }
        }

        private (VR33BSendData, TimeSpan)[] _SendDataSequence;
        public (VR33BSendData SendData, TimeSpan IntervalTimeSpan)[] SendDataSequence
        {
            get
            {
                return _SendDataSequence;
            }
        }

        public bool IsResponse(VR33BReceiveData receiveData)
        {
            if (receiveData.ReadOrWrite == VR33BMessageType.Read && receiveData.Data.Length == 17)
            {
                return true;
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public StartSampleCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0012,
                Data = new byte[] { 0, 01 }
            };
            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 1500)) };
        }

    }

    public class StopSampleCommand : ICommand
    {
        public int MaximumRepeatCount
        {
            get
            {
                return 100;
            }
        }


        private (VR33BSendData, TimeSpan)[] _SendDataSequence;
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
                return true;
            }
            return false;
        }

        public bool OnTimeout()
        {
            return false;
        }

        public StopSampleCommand(VR33BTerminal vr33bTerminal)
        {
            var sendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x012,
                Data = new byte[] { 0, 0 }
            };
            var confirmSendData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0017,
                Data = new byte[] { 0, 0 }
            };
            _SendDataSequence = new (VR33BSendData, TimeSpan)[] { (sendData, new TimeSpan(0, 0, 0, 0, 50)), (sendData, new TimeSpan(0, 0, 0, 0, 50)), (confirmSendData, new TimeSpan(0, 0, 0, 0, 50)) };
        }
    }

    public class SetSampleFrequencyCommand:ICommand
    {
        public VR33BSampleFrequence SampleFrequency { get; set; }
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
                VR33BSampleFrequence receivedSampleFrequency = (VR33BSampleFrequence)BitConverter.ToInt16(new byte[] { receiveData.Data[1], receiveData.Data[0] }, 0);
                if(receivedSampleFrequency == SampleFrequency)
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

        public SetSampleFrequencyCommand(VR33BTerminal vr33bTerminal, VR33BSampleFrequence sampleFrequency)
        {
            SampleFrequency = sampleFrequency;
            UInt16 sampleFrequncyCode;
            switch(SampleFrequency)
            {
                case VR33BSampleFrequence._1Hz:
                    sampleFrequncyCode = 1;
                    break;
                case VR33BSampleFrequence._4Hz:
                    sampleFrequncyCode = 4;
                    break;
                case VR33BSampleFrequence._16Hz:
                    sampleFrequncyCode = 0x0016;
                    break;
                case VR33BSampleFrequence._64Hz:
                    sampleFrequncyCode = 0x0064;
                    break;
                case VR33BSampleFrequence._128Hz:
                    sampleFrequncyCode = 0x0128;
                    break;
                case VR33BSampleFrequence._256Hz:
                    sampleFrequncyCode = 0x0256;
                    break;
                default:
                    sampleFrequncyCode = 1;
                    break;
            }
            var setData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Write,
                RegisterAddress = 0x0017,
                Data = new byte[] { BitConverter.GetBytes(sampleFrequncyCode)[1], BitConverter.GetBytes(sampleFrequncyCode)[0] }
            };
            var readData = new VR33BSendData
            {
                DeviceAddress = vr33bTerminal.LatestSetting.DeviceAddress,
                ReadOrWrite = VR33BMessageType.Read,
                RegisterAddress = 0x0017,
                Data = new byte[] { 0, 0 }
            };

            _SendDataSequence = new (VR33BSendData, TimeSpan)[]
            {
                (setData, new TimeSpan(0, 0, 0, 0, 100)),
                (readData, new TimeSpan(0, 0, 0, 0, 50))
            };
        }
    }
}
