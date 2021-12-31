using BC2G.Graph;

namespace BC2G.Serializers
{
    public abstract class SerializerBase
    {
        public abstract void Serialize(GraphBase g, string baseFilename);

        public abstract GraphBase Deserialize(string path, int blockHeight);
    }
}
