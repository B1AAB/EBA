using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.CLI
{
    public class BlockStatus : StatusBase
    {
        public Stopwatch Stopwatch { set; get; }

        public BlockStatus()
        {
            Stopwatch = new Stopwatch();
        }
    }
}
