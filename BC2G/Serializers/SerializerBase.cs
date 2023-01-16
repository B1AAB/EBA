namespace BC2G.Serializers
{
    public abstract class SerializerBase
    {
        public abstract void Serialize(BlockGraph g, string baseFilename);

        public abstract BlockGraph Deserialize(string path, int blockHeight);
    }
}
