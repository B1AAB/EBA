using System.Collections.Concurrent;
using System.Text;

namespace BC2G.Serializers
{
    public class AddressToIdMapper : ConcurrentDictionary<string, int>, IDisposable
    {
        private const string _delimiter = "\t";
        private const string _tmpFilenamePostfix = ".tmp";
        private readonly string _filename = string.Empty;
        private bool disposed = false;

        public AddressToIdMapper()
        { }

        public AddressToIdMapper(string filename)
        {
            _filename = filename;
            if (!string.IsNullOrEmpty(filename) && File.Exists(filename))
                Deserialize(filename);
        }

        public int GetId(string address)
        {
            return GetOrAdd(address, Count);
        }

        public void Serialize(string filename)
        {
            var builder = new StringBuilder();
            var e = GetEnumerator();
            while (e.MoveNext())
                builder.AppendLine(
                    $"{e.Current.Value}{_delimiter}{e.Current.Key}");

            File.WriteAllText(filename, builder.ToString());
        }

        public void Deserialize(string filename)
        {
            using var reader = new StreamReader(filename);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var sLine = line.Split(_delimiter);
                if (sLine.Length != 2)
                    throw new FormatException(
                        $"Expected two columns, found {sLine.Length}: {line}");
                TryAdd(sLine[1], int.Parse(sLine[0]));
            }
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
            if (!disposed)
            {
                if (disposing)
                {
                    if (!string.IsNullOrEmpty(_filename))
                    {
                        var tmpMF = _filename + _tmpFilenamePostfix;
                        Serialize(tmpMF);
                        File.Move(tmpMF, _filename, true);
                    }
                }
            }

            disposed = true;
        }
    }
}
