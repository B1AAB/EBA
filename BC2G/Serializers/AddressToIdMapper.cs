using System.Collections.Concurrent;

namespace BC2G.Serializers
{
    public class AddressToIdMapper : PersistentObject<(string, string)>
    {
        private const string _delimiter = "\t";

        private readonly object _locker = new();

        private readonly ConcurrentDictionary<string, string> _mappings;

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
            ConcurrentDictionary<string, string> mappings,
            CancellationToken cancellationToken) : base(
                filename,
                cancellationToken)
        {
            _mappings = mappings;
        }

        public string GetId(string address)
        {
            lock (_locker)
            {
                // Potential ID is set to hex of number of items 
                // currently in the _mappings dictionary.
                var potentialId = _mappings.Count.ToString("X");
                var id = _mappings.GetOrAdd(address, potentialId);
                if (id == potentialId)
                    Enqueue((id, address));
                return id;
            }
        }

        public static ConcurrentDictionary<string, string> Deserialize(string filename)
        {
            var mappings = new ConcurrentDictionary<string, string>();

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
                mappings.TryAdd(sLine[1], sLine[0]);
            }

            return mappings;
        }

        public override string Serialize((string, string) obj, CancellationToken cT)
        {
            return $"{obj.Item1}{_delimiter}{obj.Item2}{Environment.NewLine}";
        }
    }
}
