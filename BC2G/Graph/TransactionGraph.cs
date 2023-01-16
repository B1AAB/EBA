namespace BC2G.Graph;

public class TransactionGraph : GraphBase
{
    public TransactionGraph() : base()
    { }

    public double TotalInputValue { set; get; }
    public double TotalOutputValue { set; get; }
    public double Fee { set; get; }

    public ConcurrentDictionary<Node, double> Sources { set; get; } = new();
    public ConcurrentDictionary<Node, double> Targets { set; get; } = new();

    public Node AddSource(Node source, double value)
    {
        TotalInputValue += value;
        return AddOrUpdate(Sources, source, value);
    }

    public Node AddTarget(Node target, double value)
    {
        TotalOutputValue += value;
        return AddOrUpdate(Targets, target, value);
    }

    private static Node AddOrUpdate(
        ConcurrentDictionary<Node, double> collection,
        Node node,
        double value)
    {
        collection.AddOrUpdate(
            node, Utilities.Round(value),
            (_, oldValue) => Utilities.Round(oldValue + value));

        return node;
    }
}
