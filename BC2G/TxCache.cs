using BC2G.DTO;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G
{
    public class TxCache : IDisposable
    {
        public ConcurrentDictionary<string, OutputDTO> Utxo = new();

        public ConcurrentBag<string> RecentTx = new();

        private readonly string _utxoFilename = "utxo.csv";
        private readonly string _txCacheFilename = "tx_dto.csv";
        private const string _tmpFilenamePostfix = ".tmp";

        private readonly string _outputDir;
        private bool _disposed = false;
        private const string _txidVoutDelimiter = "___";
        private const char _delimiter = '\t';

        public TxCache(string outputDir)
        {
            _outputDir = outputDir;
            _utxoFilename = Path.Combine(outputDir, _utxoFilename);
            if (File.Exists(_utxoFilename))
                Deserialize();
            else
                File.Create(_utxoFilename).Dispose();

            _txCacheFilename = Path.Combine(_outputDir, _txCacheFilename);
            if (!File.Exists(_txCacheFilename))
            {
                var builder = new StringBuilder();
                builder.AppendLine(
                    $"txid{_delimiter}" +
                    $"vout{_delimiter}" +
                    $"address{_delimiter}" +
                    $"value");
                File.WriteAllText(_txCacheFilename, builder.ToString());
            }
        }

        public bool TryGet(string txid, int outputIndex, out string address, out double value)
        {
            var res = Utxo.TryRemove(ComposeId(txid, outputIndex), out OutputDTO output);
            if (res)
            {
                address = output.Address;
                value = output.Value;
            }
            else
            {
                address = string.Empty;
                value = 0;
            }
            return res;
        }

        public void Add(string txid, int outputIndex, string address, double value)
        {
            Utxo.TryAdd(
                ComposeId(txid, outputIndex),
                new OutputDTO(address, value));

            RecentTx.Add(
                $"{txid}{_delimiter}" +
                $"{outputIndex}{_delimiter}" +
                $"{address}{_delimiter}" +
                $"{value}");
        }

        private static string ComposeId(string txid, int outputIndex)
        {
            return $"{txid}{_txidVoutDelimiter}{outputIndex}";
        }

        private void Serialize(string filename)
        {
            var builder = new StringBuilder();
            foreach (var item in Utxo)
                builder.AppendLine(
                    $"{item.Key}{_delimiter}" +
                    $"{item.Value.Address}{_delimiter}" +
                    $"{item.Value.Value}");
            File.WriteAllText(filename, builder.ToString());
        }

        private void SerializeCache(string filename)
        {
            var builder = new StringBuilder();
            foreach (var item in RecentTx)
                builder.AppendLine(item);
            File.AppendAllText(filename, builder.ToString());
        }

        private void Deserialize()
        {
            using var reader = new StreamReader(_utxoFilename);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var sLine = line.Split(_delimiter);
                Utxo.TryAdd(sLine[0], new OutputDTO(sLine[1], double.Parse(sLine[2])));
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
            if (!_disposed)
            {
                if (disposing)
                {
                    var tmpMF = _utxoFilename + _tmpFilenamePostfix;
                    Serialize(tmpMF);
                    File.Move(tmpMF, _utxoFilename, true);

                    var tmpCache = _txCacheFilename + _tmpFilenamePostfix;
                    SerializeCache(tmpCache);
                    File.Move(tmpCache, _txCacheFilename, true);
                }
            }

            _disposed = true;
        }
    }
}
