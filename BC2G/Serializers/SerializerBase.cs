using BC2G.Graph;
using BC2G.Model;

namespace BC2G.Serializers
{
    public abstract class SerializerBase
    {
        public abstract void Serialize(GraphBase g, string baseFilename);//, BlockStatistics stats);

        public abstract GraphBase Deserialize(string path, int blockHeight);
    }
}
