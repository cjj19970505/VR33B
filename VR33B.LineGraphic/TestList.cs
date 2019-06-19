using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VR33B.LineGraphic
{
    class TestList : IEnumerable<VR33BSampleValue>
    {
        public IEnumerator<VR33BSampleValue> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
