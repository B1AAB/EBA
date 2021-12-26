using BC2G.Graph;

namespace BC2G.Serializers
{
    internal abstract class BaseSerializer
    {
        public AddressToIdMapper Mapper { set; get; }

        public BaseSerializer(string addressToIdMappingsFilename)
        {
            Mapper = new AddressToIdMapper(addressToIdMappingsFilename);
        }

        public abstract void Serialize(BlockGraph g, string baseFilename);
    }
}
