using BC2G.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Graph
{
    public class TransactionGraph : GraphBase
    {
        public double TotalInputValue;
        public double TotalOutputValue;

        // Using the AddOrUpdate method is the main reason to use a concurrent collection.
        public readonly ConcurrentDictionary<string, double> sources = new();
        public readonly ConcurrentDictionary<string, double> targets = new();

        public new string AddSource(string source, double value)
        {
            TotalInputValue += value;
            return AddInOrOut(sources, source, value);
        }

        public new string AddTarget(string target, double value)
        {
            TotalOutputValue += value;
            return AddInOrOut(targets, target, value);
        }

        private static string AddInOrOut(
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
