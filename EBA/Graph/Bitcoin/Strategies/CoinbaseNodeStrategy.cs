namespace EBA.Graph.Bitcoin.Strategies;

public class CoinbaseNodeStrategy(bool serializeCompressed) : BitcoinStrategyBase(serializeCompressed)
{
    public static string IdSpace { get; } = CoinbaseNode.Kind.ToString();

    public override string GetCsvHeader()
    {
        return string.Join('\t', $"{CoinbaseNode.Kind}:ID({CoinbaseNode.Kind})", ":LABEL");
    }

    public override string GetCsvRow(IGraphElement element)
    {
        return string.Join('\t', $"{CoinbaseNode.Kind}", $"{CoinbaseNode.Kind}");
    }

    public override string GetQuery(string filename)
    {
        throw new NotImplementedException();
    }
}
