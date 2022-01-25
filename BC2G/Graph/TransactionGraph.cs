using System.Collections.Concurrent;

namespace BC2G.Graph
{
    public class TransactionGraph : GraphBase
    {
        public double TotalInputValue { set; get; }
        public double TotalOutputValue { set; get; }

        public ConcurrentDictionary<string, double> Sources { set; get; } = new();
        public ConcurrentDictionary<string, double> Targets { set; get; } = new();

        public string AddSource(string source, double value)
        {
            TotalInputValue += value;
            return AddOrUpdate(Sources, source, value);
        }

        public string AddTarget(string target, double value)
        {
            TotalOutputValue += value;
            return AddOrUpdate(Targets, target, value);
        }

        private static string AddOrUpdate(
            ConcurrentDictionary<string, double> collection,
            string address,
            double value)
        {
            collection.AddOrUpdate(
                address, Utilities.Round(value),
                (_, oldValue) => Utilities.Round(oldValue + value));

            return address;
        }
    }
}
