using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B
{
    public interface IVR33BStorage
    {
        event EventHandler<VR33BSampleValue> Updated;
        VR33BTerminal VR33BTerminal { get; set; }
        Task<List<VR33BSampleValue>> GetFromDateTimeRangeAsync(DateTime startDateTime, DateTime endDateTime);
    }
}
