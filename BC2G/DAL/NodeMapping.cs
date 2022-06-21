using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL
{
    internal class NodeMapping
    {
        public List<string> Labels { set; get; } = new();

        public Dictionary<string, string> PropertyMappings { set; get; } = new();
    }
}
