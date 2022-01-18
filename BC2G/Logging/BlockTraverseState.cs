using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Logging
{
    public enum BlockTraverseState
    {
        Started = 0,
        Running = 1,
        Succeeded = 2,
        Aborted = 3,
    }
}
