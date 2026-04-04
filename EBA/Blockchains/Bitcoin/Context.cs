using EBA.Utilities;

namespace EBA.Blockchains.Bitcoin
{
    public static class BitcoinContext
    {
        public static ConcurrentDictionary<long, OHLCV> OHLCVCache = new();
    }
}
