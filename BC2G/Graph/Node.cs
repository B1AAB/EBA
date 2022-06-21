using BC2G.Blockchains;
using BC2G.Model;

namespace BC2G.Graph
{
    public class Node : IComparable<Node>, IEquatable<Node>
    {
        public string Id { get; } = "0";
        public string Address { get; } = BitcoinAgent.Coinbase;
        public ScriptType ScriptType { get; } = ScriptType.Coinbase;

        public static string Header
        {
            get
            {
                return string.Join(_delimiter, new string[]
                {
                    "Id",
                    "ScriptType"
                });
            }
        }

        private const string _delimiter = "\t";

        /// <summary>
        /// This constructor creates the Coinbase node.
        /// </summary>
        public Node() { }

        public Node(string id, string address, ScriptType scriptType)
        {
            Id = id;
            Address = address;
            ScriptType = scriptType;
        }

        public override string ToString()
        {
            return string.Join(
                _delimiter, 
                new string[] { Id, ScriptType.ToString("d") });
        }

        public override int GetHashCode()
        {
            // Do not add ID here, because ID is generated at
            // runtime in a multi-threaded process, hence cannot
            // guarantee a node's ID is reproducible..
            return HashCode.Combine(Address, ScriptType);
        }

        public int CompareTo(Node? other)
        {
            if (other == null) return -1;
            var r = Address.CompareTo(other.Address);
            if (r != 0) return r;
            return ScriptType.CompareTo(other.ScriptType);
        }

        
        public bool Equals(Node? other)
        {
            if (other == null) 
                return false;

            return Address == other.Address && ScriptType == other.ScriptType;
        }
    }
}
