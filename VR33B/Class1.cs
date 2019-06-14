using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.HashFunction;
using System.Data.HashFunction.CRC;


namespace VR33B
{
    public class Class1
    {
        
        public void Test()
        {
            
            CRCConfig config = new CRCConfig();
            config.HashSizeInBits = 16;
            var a = CRCFactory.Instance.Create();
        }
    }

    
}
