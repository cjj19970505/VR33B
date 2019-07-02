using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace VR33B.Storage
{
    /// <summary>
    /// 更新数据采取的策略：
    /// 更新的数据先储存到内存中（InMemory），InMemory的数据量到达InMemoryBufferSize时，移交一部分内存中的数据(MemoryToDBBatchSize)到DataBase中
    /// </summary>
    public class VR33BSqliteStorage : IVR33BStorage
    {

        public const string SettingFileName = "VR33BSqliteStoragetSetting.xml";

        VR33BTerminal _VR33BTerminal;

        private VR33BSampleProcess _CurrentSampleProcess;

        public VR33BSqliteStorageSetting Setting { get; }

        private object _BeforeStoreBufferLock;
        private List<VR33BSampleValueEntity> _BeforeStoreBuffer;

        //private int _InMemoryBufferSize = 1000;
        private ReaderWriterLockSlim _InMemoryBufferLock;
        private List<VR33BSampleValueEntity> _InMemoryBuffer;

        public VR33BTerminal VR33BTerminal
        {
            get
            {
                return _VR33BTerminal;
            }
            set
            {
                if (_VR33BTerminal != null)
                {
                    _VR33BTerminal.OnVR33BSampleStarted -= _VR33BTerminal_OnVR33BSampleStarted;
                    //_VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                    _VR33BTerminal.OnVR33BSampleEnded -= _VR33BTerminal_OnVR33BSampleEnded;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                //_VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
            }
        }
        private VR33BSampleTimeDispatcher _SampleTimeDispatcher;

        private ReaderWriterLockSlim _DataContextLock;
        private VR33BSqliteStorageContext _DataContext;
        private object _MemoryToDataBaseTransferLock;
        public VR33BSampleTimeDispatcher SampleTimeDispatcher
        {
            get
            {
                return _SampleTimeDispatcher;
            }
            set
            {
                if (_SampleTimeDispatcher != null)
                {
                    _SampleTimeDispatcher.OnSampleValueTimeDispatched -= _VR33BTerminal_OnVR33BSampleValueReceived;
                }
                _SampleTimeDispatcher = value;
                _SampleTimeDispatcher.OnSampleValueTimeDispatched += _VR33BTerminal_OnVR33BSampleValueReceived;
            }
        }

        public VR33BSqliteStorage()
        {
            var defaultSetting = VR33BSqliteStorageSetting.Default;

            var fileName = SettingFileName;
            var filePath = Environment.CurrentDirectory + "//" + fileName;
            XmlSerializer serializer = new XmlSerializer(typeof(VR33BSqliteStorageSetting));
            if (!File.Exists(filePath))
            {
                using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    serializer.Serialize(settingStream, defaultSetting);
                };
                Setting = defaultSetting;
            }
            else
            {
                using (FileStream settingStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    Setting = (VR33BSqliteStorageSetting)serializer.Deserialize(settingStream);
                };
            }

            _DataContext = new VR33BSqliteStorageContext();
            _BeforeStoreBuffer = new List<VR33BSampleValueEntity>();
            _InMemoryBuffer = new List<VR33BSampleValueEntity>();
            _BeforeStoreBufferLock = new object();
            _DataContextLock = new ReaderWriterLockSlim();
            _InMemoryBufferLock = new ReaderWriterLockSlim();
            _MemoryToDataBaseTransferLock = new object();

        }

        private async void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                lock (_MemoryToDataBaseTransferLock)
                {
                    _DataContextLock.EnterWriteLock();
                    {

                        _InMemoryBufferLock.EnterWriteLock();
                        {
                            _DataContext.SampleValueEntities.AddRange(_InMemoryBuffer);
                            _InMemoryBuffer.Clear();
                        }
                        _InMemoryBufferLock.ExitWriteLock();

                        _DataContext.SaveChanges();

                    }
                    _DataContextLock.ExitWriteLock();
                }
            });
        }

        private bool _TransferingDataToDatabase = false;
        private async void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            lock (_BeforeStoreBufferLock)
            {
                _BeforeStoreBuffer.Add(VR33BSampleValueEntity.FromStruct(e));
            }
            if (_InMemoryBufferLock.WaitingWriteCount > 2)
            {
                return;
            }
            await Task.Run(() =>
            {
                int inMemorySize;
                _InMemoryBufferLock.EnterWriteLock();
                {
                    lock (_BeforeStoreBufferLock)
                    {
                        _InMemoryBuffer.AddRange(_BeforeStoreBuffer);
                        _BeforeStoreBuffer.Clear();
                    }
                    inMemorySize = _InMemoryBuffer.Count;
                }
                _InMemoryBufferLock.ExitWriteLock();
                if (inMemorySize > Setting.InMemoryBufferSize && !_TransferingDataToDatabase) //添加!_DataContextLock.IsWriteLockHeld这个条件可能会使后面数据库增长到很大时，InMemory中的条目数越来越多 
                {
                    Task.Run(() =>
                    {
                        lock (_MemoryToDataBaseTransferLock)
                        {
                            _TransferingDataToDatabase = true;
                            List<VR33BSampleValueEntity> memToDBMiddleBuffer;

                            _InMemoryBufferLock.EnterReadLock();
                            inMemorySize = _InMemoryBuffer.Count;
                            if(inMemorySize <= Setting.InMemoryBufferSize)
                            {
                                _InMemoryBufferLock.ExitReadLock();
                            }
                            else
                            {
                                memToDBMiddleBuffer = _InMemoryBuffer.GetRange(0, Setting.MemoryToDBBatchSize);
                                _InMemoryBufferLock.ExitReadLock();
                                _DataContextLock.EnterWriteLock();
                                {
                                    _TransferingDataToDatabase = true;
                                    DateTime beginTimingDateTime = DateTime.Now;
                                    System.Diagnostics.Debug.WriteLine("BEGIN MOVING MEMORY TO DB");
                                    _DataContext.SampleValueEntities.AddRange(memToDBMiddleBuffer);
                                    _DataContext.SaveChanges();
                                    System.Diagnostics.Debug.WriteLine("MOVE MEMORY TO DB TAKES " + (DateTime.Now - beginTimingDateTime).TotalMilliseconds + "Ms");
                                    _TransferingDataToDatabase = false;
                                }
                                _DataContextLock.ExitWriteLock();

                                _InMemoryBufferLock.EnterWriteLock();
                                {
                                    _InMemoryBuffer.RemoveRange(0, Setting.MemoryToDBBatchSize);
                                }
                                _InMemoryBufferLock.ExitWriteLock();
                            }
                            _TransferingDataToDatabase = false;
                        }
                    });

                }

                Updated?.Invoke(this, e);
            });


            //throw new NotImplementedException();
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, VR33BSampleProcess e)
        {
            _CurrentSampleProcess = e;
            Task.Run(() =>
            {
                _DataContextLock.EnterWriteLock();
                {
                    _DataContext.SampleProcessEntities.Add(VR33BSampleProcessEntity.FromStruct(_CurrentSampleProcess));
                    _DataContext.SaveChanges();
                }
                _DataContextLock.ExitWriteLock();
            });
        }

        public event EventHandler<VR33BSampleValue> Updated;

        public Task<List<VR33BSampleValue>> GetFromDateTimeRangeAsync(DateTime startDateTime, DateTime endDateTime)
        {
            return Task.Run(() =>
            {
                List<VR33BSampleValue> inMemoryQueryResult = new List<VR33BSampleValue>();
                List<VR33BSampleValue> inDatabaseQueryResult = new List<VR33BSampleValue>();
                bool inDatabaseQueryNeeded = true;

                _InMemoryBufferLock.EnterReadLock();
                {
                    if (_InMemoryBuffer.Count != 0)
                    {
                        if (startDateTime > _InMemoryBuffer.First().SampleDateTime /*&& endDateTime <= _InMemoryBuffer.Last().SampleDateTime*/)
                        {
                            inDatabaseQueryNeeded = false;
                        }
                        inMemoryQueryResult.AddRange(
                            (from entity in _InMemoryBuffer
                             where entity.SampleDateTime >= startDateTime && entity.SampleDateTime <= endDateTime
                             select entity.ToStruct()).ToList()
                            );
                    }
                }
                _InMemoryBufferLock.ExitReadLock();

                if (inDatabaseQueryNeeded)
                {
                    using (VR33BSqliteStorageContext dbcontext = new VR33BSqliteStorageContext())
                    {
                        _DataContextLock.EnterReadLock();
                        {
                            DateTime beforeReadDateTime = DateTime.Now;
                            System.Diagnostics.Debug.WriteLine("BEGIN QUERY FROM DB");
                            inDatabaseQueryResult.AddRange(
                                (from entity in dbcontext.SampleValueEntities
                                 where entity.SampleDateTime >= startDateTime && entity.SampleDateTime <= endDateTime && entity.SampleProcessGuid == _CurrentSampleProcess.Guid
                                 select entity.ToStruct()).ToList()
                                );
                            System.Diagnostics.Debug.WriteLine("QUERY FROM DB TAKES " + (DateTime.Now - beforeReadDateTime).TotalMilliseconds + "Ms");
                        }
                        _DataContextLock.ExitReadLock();
                    }
                    inDatabaseQueryResult.Sort((value1, value2) =>
                        {
                            if (value1.SampleIndex < value2.SampleIndex)
                            {
                                return -1;
                            }
                            else if (value1.SampleIndex == value2.SampleIndex)
                            {
                                return 0;
                            }
                            else
                            {
                                return 1;
                            }

                        });

                }
                var list = new List<VR33BSampleValue>();
                list.AddRange(inDatabaseQueryResult);
                list.AddRange(inMemoryQueryResult);
                return list;

            });
        }

        public Task<List<VR33BSampleValue>> GetFromSampleIndexRangeAsync(long minIndex, long maxIndex)
        {
            return Task.Run(() =>
            {
                List<VR33BSampleValue> inMemoryQueryResult = new List<VR33BSampleValue>();
                List<VR33BSampleValue> inDatabaseQueryResult = new List<VR33BSampleValue>();
                bool inDatabaseQueryNeeded = true;
                _InMemoryBufferLock.EnterReadLock();
                {
                    if (_InMemoryBuffer.Count > 0)
                    {
                        if (minIndex >= _InMemoryBuffer.First().SampleIndex && maxIndex <= _InMemoryBuffer.Last().SampleIndex)
                        {
                            inDatabaseQueryNeeded = false;
                        }
                        inMemoryQueryResult = (from entity in _InMemoryBuffer
                                               where entity.SampleIndex >= minIndex && entity.SampleIndex <= maxIndex
                                               select entity.ToStruct()).ToList();
                        inMemoryQueryResult.Sort((value1, value2) =>
                        {
                            if (value1.SampleIndex < value2.SampleIndex)
                            {
                                return -1;
                            }
                            else if (value1.SampleIndex == value2.SampleIndex)
                            {
                                return 0;
                            }
                            else
                            {
                                return 1;
                            }

                        });
                    }
                }
                _InMemoryBufferLock.ExitReadLock();

                if (inDatabaseQueryNeeded)
                {
                    using (var dbcontext = new VR33BSqliteStorageContext())
                    {
                        _DataContextLock.EnterReadLock();
                        {
                            inDatabaseQueryResult.AddRange((from entity in dbcontext.SampleValueEntities
                                                            where entity.SampleIndex >= minIndex && entity.SampleIndex <= maxIndex
                                                            select entity.ToStruct()).ToList());
                        }
                        _DataContextLock.ExitReadLock();
                    }
                    inDatabaseQueryResult.Sort((value1, value2) =>
                    {
                        if (value1.SampleIndex < value2.SampleIndex)
                        {
                            return -1;
                        }
                        else if (value1.SampleIndex == value2.SampleIndex)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1;
                        }

                    });

                }
                var list = new List<VR33BSampleValue>();
                list.AddRange(inDatabaseQueryResult);
                list.AddRange(inMemoryQueryResult);
                return list;
            });
        }

        public async Task<List<VR33BSampleProcess>> GetAllSampleProcessesAsync()
        {
            var processList = await (from processEntity in _DataContext.SampleProcessEntities
                                     select processEntity.ToStruct()).ToListAsync();
            return processList;
        }
    }

    internal class VR33BSqliteStorageContext : DbContext
    {
        private static bool _Created = false;
        public DbSet<VR33BSampleValueEntity> SampleValueEntities { get; set; }
        public DbSet<VR33BSampleProcessEntity> SampleProcessEntities { get; set; }
        public VR33BSqliteStorageContext()
        {
            if (!_Created)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
                _Created = true;
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseSqlite(@"Data Source=TestDB.db");
        }
    }

    internal class VR33BSampleValueEntity
    {
        //public long Id { get; set; }
        [System.ComponentModel.DataAnnotations.Key]
        public long Id { get; set; }
        public Guid SampleProcessGuid { get; set; }
        public long SampleIndex { get; set; }
        public DateTime SampleDateTime { get; set; }
        public UInt16 RawAccelerometerValueX { get; set; }
        public UInt16 RawAccelerometerValueY { get; set; }
        public UInt16 RawAccelerometerValueZ { get; set; }
        public UInt16 RawTemperature { get; set; }
        public UInt16 RawHumidity { get; set; }

        public double SampleTimeSpanInMs { get; set; }

        public static VR33BSampleValueEntity FromStruct(VR33BSampleValue vr33bSampleValue)
        {
            return new VR33BSampleValueEntity
            {
                SampleProcessGuid = vr33bSampleValue.SampleProcessGuid,
                SampleIndex = vr33bSampleValue.SampleIndex,
                SampleDateTime = vr33bSampleValue.SampleDateTime,
                RawAccelerometerValueX = vr33bSampleValue.RawAccelerometerValue.X,
                RawAccelerometerValueY = vr33bSampleValue.RawAccelerometerValue.Y,
                RawAccelerometerValueZ = vr33bSampleValue.RawAccelerometerValue.Z,
                RawTemperature = vr33bSampleValue.RawTemperature,
                RawHumidity = vr33bSampleValue.RawHumidity,
                SampleTimeSpanInMs = vr33bSampleValue.SampleTimeSpanInMs
            };
        }

        public VR33BSampleValue ToStruct()
        {
            return new VR33BSampleValue
            {
                SampleIndex = this.SampleIndex,
                SampleDateTime = this.SampleDateTime,
                RawAccelerometerValue = (this.RawAccelerometerValueX, this.RawAccelerometerValueY, this.RawAccelerometerValueZ),
                RawTemperature = this.RawTemperature,
                RawHumidity = this.RawHumidity,
                SampleTimeSpanInMs = this.SampleTimeSpanInMs
            };
        }
    }
    internal class VR33BSampleProcessEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public VR33BSampleProcess ToStruct()
        {
            return new VR33BSampleProcess
            {
                Name = this.Name,
                Guid = this.Guid
            };
        }

        public static VR33BSampleProcessEntity FromStruct(VR33BSampleProcess process)
        {
            return new VR33BSampleProcessEntity
            {
                Name = process.Name,
                Guid = process.Guid
            };
        }
    }

    public struct VR33BSqliteStorageSetting
    {
        public int InMemoryBufferSize { get; set; }

        /// <summary>
        /// 当需要移交数据到DB中时要一次性移交多少数据
        /// 太小的话会造成频繁写入数据库，太大会造成一次性写入（锁定）时间太小
        /// </summary>
        public int MemoryToDBBatchSize { get; set; }

        public static VR33BSqliteStorageSetting Default
        {
            get
            {
                return new VR33BSqliteStorageSetting
                {
                    InMemoryBufferSize = 10000,
                    MemoryToDBBatchSize = 1000
                };
            }
        }
    }
}
