using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VR33B.Storage
{
    public class VR33BSqliteStorage : IVR33BStorage
    {

        VR33BTerminal _VR33BTerminal;

        private object _BeforeStoreBufferLock;
        private List<VR33BSampleValueEntity> _BeforeStoreBuffer;

        private int _InMemoryBufferSize = 1000;
        private object _InMemoryBufferLock;
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
                    _VR33BTerminal.OnVR33BSampleValueReceived -= _VR33BTerminal_OnVR33BSampleValueReceived;
                    _VR33BTerminal.OnVR33BSampleEnded -= _VR33BTerminal_OnVR33BSampleEnded;
                }
                _VR33BTerminal = value;
                _VR33BTerminal.OnVR33BSampleStarted += _VR33BTerminal_OnVR33BSampleStarted;
                _VR33BTerminal.OnVR33BSampleValueReceived += _VR33BTerminal_OnVR33BSampleValueReceived;
                _VR33BTerminal.OnVR33BSampleEnded += _VR33BTerminal_OnVR33BSampleEnded;
            }
        }

        private object _DataContextLock;
        private VR33BSqliteStorageContext _DataContext;

        public VR33BSqliteStorage()
        {
            _DataContext = new VR33BSqliteStorageContext();
            _BeforeStoreBuffer = new List<VR33BSampleValueEntity>();
            _InMemoryBuffer = new List<VR33BSampleValueEntity>();
            _BeforeStoreBufferLock = new object();
            _DataContextLock = new object();
            _InMemoryBufferLock = new object();

        }

        private void _VR33BTerminal_OnVR33BSampleEnded(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        private async void _VR33BTerminal_OnVR33BSampleValueReceived(object sender, VR33BSampleValue e)
        {
            lock(_BeforeStoreBufferLock)
            {
                _BeforeStoreBuffer.Add(VR33BSampleValueEntity.FromStruct(e));
            }
            await Task.Run(() =>
            {
                lock (_InMemoryBufferLock)
                {
                    lock (_BeforeStoreBufferLock)
                    {
                        _InMemoryBuffer.AddRange(_BeforeStoreBuffer);
                        _BeforeStoreBuffer.Clear();
                    }
                    if(_InMemoryBuffer.Count > _InMemoryBufferSize)
                    {
                        lock(_DataContextLock)
                        {
                            _DataContext.SampleValueEntities.AddRange(_InMemoryBuffer);
                            _InMemoryBuffer.Clear();
                            _DataContext.SaveChanges();
                        }
                    }
                }
                Updated?.Invoke(this, e);
            });

            
            //throw new NotImplementedException();
        }

        private void _VR33BTerminal_OnVR33BSampleStarted(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        public event EventHandler<VR33BSampleValue> Updated;

        public Task<List<VR33BSampleValue>> GetFromDateTimeRangeAsync(DateTime startDateTime, DateTime endDateTime)
        {
            return Task.Run(() =>
            {
                List<VR33BSampleValue> inMemoryQueryResult = new List<VR33BSampleValue>();
                List<VR33BSampleValue> inDatabaseQueryResult = new List<VR33BSampleValue>();
                bool inDatabaseQueryNeeded = true;
                lock (_InMemoryBufferLock)
                {
                    if (_InMemoryBuffer.Count != 0)
                    {
                        if(startDateTime > _InMemoryBuffer.First().SampleDateTime /*&& endDateTime <= _InMemoryBuffer.Last().SampleDateTime*/)
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
                if(inDatabaseQueryNeeded)
                {
                    lock(_DataContextLock)
                    {
                        inDatabaseQueryResult.AddRange(
                            (from entity in _DataContext.SampleValueEntities
                             where entity.SampleDateTime >= startDateTime && entity.SampleDateTime <= endDateTime
                             select entity.ToStruct()).ToList()
                            );
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
                }
                var list = new List<VR33BSampleValue>();
                list.AddRange(inDatabaseQueryResult);
                list.AddRange(inMemoryQueryResult);
                return list;
                
            });
        }
    }

    internal class VR33BSqliteStorageContext : DbContext
    {
        private static bool _Created = false;
        public DbSet<VR33BSampleValueEntity> SampleValueEntities { get; set; }
        public VR33BSqliteStorageContext()
        {
            if (!_Created)
            {
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
            //SampleValueEntities.OrderBy
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            
            return base.SaveChanges(acceptAllChangesOnSuccess);
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
        public long SampleIndex { get; set; }
        public DateTime SampleDateTime { get; set; }
        public UInt16 RawAccelerometerValueX { get; set; }
        public UInt16 RawAccelerometerValueY { get; set; }
        public UInt16 RawAccelerometerValueZ { get; set; }
        public UInt16 RawTemperature { get; set; }
        public UInt16 RawHumidity { get; set; }

        public static VR33BSampleValueEntity FromStruct(VR33BSampleValue vr33bSampleValue)
        {
            return new VR33BSampleValueEntity
            {
                SampleIndex = vr33bSampleValue.SampleIndex,
                SampleDateTime = vr33bSampleValue.SampleDateTime,
                RawAccelerometerValueX = vr33bSampleValue.RawAccelerometerValue.X,
                RawAccelerometerValueY = vr33bSampleValue.RawAccelerometerValue.Y,
                RawAccelerometerValueZ = vr33bSampleValue.RawAccelerometerValue.Z,
                RawTemperature = vr33bSampleValue.RawTemperature,
                RawHumidity = vr33bSampleValue.RawHumidity
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
                RawHumidity = this.RawHumidity
            };
        }
    }
}
