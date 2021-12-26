using BC2G.Model;
using System.Collections.ObjectModel;

namespace BC2G.Graph
{
    public class BaseGraph
    {
        // Note that exposing as `readonly` does not prevent anyone from casting it 
        // to the original type and making changes. For a 'true' readonly type, 
        // you may return a copy of the collection instead.
        // TODO: test this ^^
        // TODO: improve on the immutability of the collections.


        public ReadOnlyCollection<Edge> Edges { get { return _edges.AsReadOnly(); } }
        public IReadOnlyCollection<string> Nodes { get { return _nodes; } }


        private readonly List<Edge> _edges = new();
        private readonly HashSet<string> _nodes = new();

        public void AddEdge(Edge edge)
        {
            // TODO: avoid adding duplicate edges.
            _edges.Add(edge);

            if (!_nodes.Contains(edge.Source))
                _nodes.Add(edge.Source);
            if (!_nodes.Contains(edge.Target))
                _nodes.Add(edge.Target);
        }

        public void AddEdges(ReadOnlyCollection<Edge> edges)
        {
            foreach (var edge in edges)
                AddEdge(edge);
        }
    }
}
