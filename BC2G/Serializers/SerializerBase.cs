using BC2G.Blockchains.Bitcoin.Graph;

namespace BC2G.Serializers;

public abstract class SerializerBase
{
    public abstract void Serialize(BitcoinBlockGraph g, string baseFilename);

    public abstract BitcoinBlockGraph Deserialize(string path, int blockHeight);
}
