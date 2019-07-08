using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public delegate IEnumerable<VR33BSampleValue> VR33BSampleValueQueryDelegate(IEnumerable<VR33BSampleValue> sampleValueEnumerable);
    public interface IVR33BStorage
    {
        event EventHandler<VR33BSampleValue> Updated;
        VR33BTerminal VR33BTerminal { get; set; }
        Task<List<VR33BSampleValue>> GetFromDateTimeRangeAsync(DateTime startDateTime, DateTime endDateTime);
        Task<List<VR33BSampleValue>> GetFromSampleIndexRangeAsync(long minIndex, long maxIndex);

        Task<List<VR33BSampleValue>> QueryAsync(VR33BSampleValueQueryDelegate queryFunc);

        Task<List<VR33BSampleProcess>> GetAllSampleProcessAsync();

    }
}
