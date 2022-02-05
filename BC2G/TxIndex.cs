using BC2G.DTO;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G
{
    public class TxIndex : IDisposable
    {
        private readonly string _utxoIndexFilename = "utxo.csv";
        private readonly string _txIndexFilename = "tx_index.csv";
        private const string _tmpFilenamePostfix = ".tmp";

        private readonly string _outputDir;
        private bool _disposed = false;
        private const string _txidVoutDelimiter = "___";

        private const int _maxItemsInCache = 100000;
        private const int _cacheSqueezeSize = 1000;
        private readonly object _locker = new();

        private readonly TransactionIndex _txIndex;
        private readonly ConcurrentDictionary<string, TxIndexItem> _utxoIdx = new();

        private readonly Random _random = new Random();

        public TxIndex(string outputDir, CancellationToken cancellationToken)
        {
            _outputDir = outputDir;
            _utxoIndexFilename = Path.Combine(outputDir, _utxoIndexFilename);
            if (File.Exists(_utxoIndexFilename))
                Deserialize();
            else
                File.Create(_utxoIndexFilename).Dispose();

            _txIndexFilename = Path.Combine(_outputDir, _txIndexFilename);
            _txIndex = new TransactionIndex(_txIndexFilename, cancellationToken);
        }

        public bool TryGet(string txid, int outputIndex, out string address, out double value)
        {
            var res = _utxoIdx.TryRemove(
                ComposeId(txid, outputIndex), 
                out TxIndexItem output);

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
            if (_utxoIdx.Count >= _maxItemsInCache)
            {
                // TODO: how this can be improved?!
                lock (_locker)
                {
                    var keys = _utxoIdx.Keys;
                    var removedKeys = new HashSet<string>();
                    for (int i = 0; i < _cacheSqueezeSize; i++)
                    {
                        string item;
                        do { item = keys.ElementAt(_random.Next(0, keys.Count - 1)); }
                        while (removedKeys.Contains(item));
                        removedKeys.Add(item);

                        _utxoIdx.TryRemove(item, out TxIndexItem _);
                    }
                }
            }

            _utxoIdx.TryAdd(
                ComposeId(txid, outputIndex),
                new TxIndexItem(address, value));

            _txIndex.Enqueue(new TxIndexItem(address, value, txid, outputIndex));
        }

        private static string ComposeId(string txid, int outputIndex)
        {
            return $"{txid}{_txidVoutDelimiter}{outputIndex}";
        }

        private void Serialize(string filename)
        {
            var builder = new StringBuilder();
            foreach (var item in _utxoIdx)
                builder.Append(
                    new TxIndexItem(
                        address: item.Value.Address,
                        value: item.Value.Value,
                        txid: item.Key)
                    .ToString());

            File.WriteAllText(filename, builder.ToString());
        }

        private void Deserialize()
        {
            using var reader = new StreamReader(_utxoIndexFilename);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var item = TxIndexItem.Deserialize(line);
                
                // Note that the deserialized item will be different
                // from the item in that the item before serialization 
                // had its `txid=""` and txid was the key of the collection,
                // however, after it is deserialized, txid is set to 
                // the transaction ID. Hence, the size of the deserialized
                // object is larger than when it was at serialization. 
                // If this turns out to be an issue, should rework
                // deserialization.
                _utxoIdx.TryAdd(item.TxId, item);
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
                    var tmpMF = _utxoIndexFilename + _tmpFilenamePostfix;
                    Serialize(tmpMF);
                    File.Move(tmpMF, _utxoIndexFilename, true);
                }
            }

            _disposed = true;
        }
    }
}
