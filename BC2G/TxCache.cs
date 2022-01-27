using BC2G.DTO;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G
{
    public class TxCache : IDisposable
    {
        public ConcurrentDictionary<string, OutputDTO> Utxo = new();

        private readonly string _utxoFilename = "utxo.csv";
        private readonly string _txIndexFilename = "tx_index.csv";
        private const string _tmpFilenamePostfix = ".tmp";

        private readonly string _outputDir;
        private bool _disposed = false;
        private const string _txidVoutDelimiter = "___";
        private const char _delimiter = '\t';

        private readonly TransactionIndex _txIndex;

        public TxCache(string outputDir, CancellationToken cancellationToken)
        {
            _outputDir = outputDir;
            _utxoFilename = Path.Combine(outputDir, _utxoFilename);
            if (File.Exists(_utxoFilename))
                Deserialize();
            else
                File.Create(_utxoFilename).Dispose();

            _txIndexFilename = Path.Combine(_outputDir, _txIndexFilename);
            if (!File.Exists(_txIndexFilename))
            {
                var builder = new StringBuilder();
                builder.AppendLine(
                    $"txid{_delimiter}" +
                    $"vout{_delimiter}" +
                    $"address{_delimiter}" +
                    $"value");
                File.WriteAllText(_txIndexFilename, builder.ToString());
            }

            _txIndex = new TransactionIndex(_txIndexFilename, cancellationToken);
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

            _txIndex.Enqueue(new TransactionIndexItem(txid, outputIndex, address, value));
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
                }
            }

            _disposed = true;
        }
    }
}
