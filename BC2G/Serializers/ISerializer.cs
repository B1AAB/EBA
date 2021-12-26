using BC2G.Graph;

namespace BC2G.Serializers
{
    internal interface ISerializer
    {
        void Serialize(string outputFilename, BlockGraph g, Dictionary<string, int> nodeID);
    }
}
