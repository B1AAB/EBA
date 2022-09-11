using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Model.Config
{
    public enum GraphSampleMode
    {
        A, B, C
    }

    public class GraphSampleOptions
    {
        public int Count { set; get; }
        public int Hops { set; get; }
        public GraphSampleMode Mode { set; get; } = GraphSampleMode.A;
        public int MinNodeCount { set; get; } = 3;
        public int MaxNodeCount { set; get; } = 200;
        public int MinEdgeCount { set; get; } = 3;
        public int MaxEdgeCount { set; get; } = 200;
        public double RootNodeSelectProb
        {
            set
            {
                if (value < 0 || value > 1)
                    _rootNodeSelectProb = 1;
                else
                    _rootNodeSelectProb = value;
            }
            get { return _rootNodeSelectProb; }
        }
        private double _rootNodeSelectProb = 0.1;
    }
}
