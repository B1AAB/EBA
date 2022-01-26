using System.Collections.Concurrent;

namespace BC2G.Serializers
{
    public class AddressToIdMapper : IDisposable
    {
        private const string _delimiter = "\t";
        private bool _disposed = false;

        private readonly object _locker = new();

        private readonly ConcurrentDictionary<string, int> _mappings;
        private readonly BlockingCollection<(string, int)> _buffer = new();
        private readonly StreamWriter _stream;

        public AddressToIdMapper(
            string filename,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Filename cannot be null or empty.");

            if (File.Exists(filename))
            {
                _mappings = Deserialize(filename);
            }
            else
            {
                _mappings = new();
                File.Create(filename).Dispose();
            }

            _stream = File.AppendText(filename);

            var thread = new Thread(() =>
            {
                while (true)
                {
                    (string address, int id) mapping;

                    try { mapping = _buffer.Take(cancellationToken); }
                    catch (OperationCanceledException) { break; }

                    _stream.WriteLine($"{mapping.id}{_delimiter}{mapping.address}");
                }
            })
            {
                IsBackground = false
            };
            thread.Start();
        }

        public int GetId(string address)
        {
            lock (_locker)
            {
                var id = _mappings.GetOrAdd(address, _mappings.Count);
                if (id == _mappings.Count)
                    _buffer.Add((address, id));
                return id;
            }
        }

        private static ConcurrentDictionary<string, int> Deserialize(string filename)
        {
            var mappings = new ConcurrentDictionary<string, int>();

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

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _stream.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
