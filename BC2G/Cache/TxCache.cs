using BC2G.DTO;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G
{
    // TODO: this could be better implemented using a 
    // database; e.g., RavenDb (https://ravendb.net).

    public class TxCache : IDisposable
    {
        public bool CanClose
        {
            get { return _visitedTxCache.CanDispose; }
        }

        private readonly string _utxoIndexFilename = "utxo.csv";
        private readonly string _txIndexFilename = "tx_index.csv";
        private const string _tmpFilenamePostfix = ".tmp";

        private readonly string _outputDir;
        private bool _disposed = false;
        private const string _txidVoutDelimiter = "___";

        private const int _maxItemsInCache = 1000000;
        private const int _cacheSqueezeSize = 1000;
        private readonly object _locker = new();

        private readonly PersistentObject<TxCacheItem> _visitedTxCache;
        private readonly ConcurrentDictionary<string, TxCacheItem> _utxoCache = new();

        private readonly Random _random = new();

        public TxCache(string outputDir, CancellationToken cT)
        {
            _outputDir = outputDir;
            _utxoIndexFilename = Path.Combine(outputDir, _utxoIndexFilename);
            if (File.Exists(_utxoIndexFilename))
                Deserialize();
            else
                File.Create(_utxoIndexFilename).Dispose();

            _txIndexFilename = Path.Combine(_outputDir, _txIndexFilename);
            _visitedTxCache = new PersistentObject<TxCacheItem>(
                _txIndexFilename, cT, TxCacheItem.GetHeader());
        }

        public bool TryGet(string txid, int outputIndex, out string address, out double value)
        {
            var res = _utxoCache.TryRemove(
                ComposeId(txid, outputIndex), 
                out TxCacheItem output);

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
            if (_utxoCache.Count >= _maxItemsInCache)
            {
                // TODO: how this can be improved?!
                lock (_locker)
                {
                    var keys = _utxoCache.Keys;
                    var removedKeys = new HashSet<string>();
                    for (int i = 0; i < _cacheSqueezeSize; i++)
                    {
                        string item;
                        do { item = keys.ElementAt(_random.Next(0, keys.Count - 1)); }
                        while (removedKeys.Contains(item));
                        removedKeys.Add(item);

                        _utxoCache.TryRemove(item, out TxCacheItem _);
                    }
                }
            }

            _utxoCache.TryAdd(
                ComposeId(txid, outputIndex),
                new TxCacheItem(address, value));

            _visitedTxCache.Enqueue(new TxCacheItem(address, value, txid, outputIndex));
        }

        private static string ComposeId(string txid, int outputIndex)
        {
            return $"{txid}{_txidVoutDelimiter}{outputIndex}";
        }

        private void Serialize(string filename)
        {
            var builder = new StringBuilder();
            foreach (var item in _utxoCache)
                builder.Append(
                    new TxCacheItem(
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
                var item = TxCacheItem.Deserialize(line);
                
                // Note that the deserialized item will be different
                // from the item in that the item before serialization 
                // had its `txid=""` and txid was the key of the collection,
                // however, after it is deserialized, txid is set to 
                // the transaction ID. Hence, the size of the deserialized
                // object is larger than when it was at serialization. 
                // If this turns out to be an issue, should rework
                // deserialization.
                _utxoCache.TryAdd(item.TxId, item);
            }
        }

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
                    _visitedTxCache.Dispose();
                }
            }

            _disposed = true;
        }
    }
}
