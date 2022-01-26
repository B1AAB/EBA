using System.Collections.Concurrent;

namespace BC2G.Serializers
{
    public class AddressToIdMapper : PersistentObject<(int, string)>
    {
        private const string _delimiter = "\t";

        private readonly object _locker = new();

        private readonly ConcurrentDictionary<string, int> _mappings;

        // Cannot take the filename and call the Deserializers
        // from the constructor, because the base type needs to
        // create a write stream to the file, and since that
        // handle will be created before running the constructor
        // body, it will fail complaning the file is open in 
        // another process. There are a few hacky workarounds, 
        // but probably the most intuitive approach is to 
        // require the mappings in the constructor.

        public AddressToIdMapper(
            string filename,
            ConcurrentDictionary<string, int> mappings,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken)
        {
            _mappings = mappings;
        }

        public int GetId(string address)
        {
            lock (_locker)
            {
                var id = _mappings.GetOrAdd(address, _mappings.Count);
                if (id == _mappings.Count - 1)
                    Enqueue((id, address));
                return id;
            }
        }

        public static ConcurrentDictionary<string, int> Deserialize(string filename)
        {
            var mappings = new ConcurrentDictionary<string, int>();

            if (!File.Exists(filename))
                return mappings;

            using var reader = new StreamReader(filename);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var sLine = line.Split(_delimiter);
                if (sLine.Length != 2)
                    throw new FormatException(
                        $"Expected two columns, found {sLine.Length}: {line}");
                mappings.TryAdd(sLine[1], int.Parse(sLine[0]));
            }

            return mappings;
        }

        public override string Serialize((int, string) obj, CancellationToken cancellationToken)
        {
            return $"{obj.Item1}{_delimiter}{obj.Item2}{Environment.NewLine}";
        }
    }
}
