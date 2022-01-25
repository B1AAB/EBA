using BC2G.Graph;
using BC2G.Serializers;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G
{
    // TODO: refactor this to a more intuitive name. 
    public class AutoPersistent
    {
        private const string _delimiter = ",";
        private readonly BlockingCollection<GraphBase> _buffer = new();

        public AutoPersistent(
            string filename, 
            AddressToIdMapper mapper, 
            CancellationToken cancellationToken)
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    GraphBase g;

                    try { g = _buffer.Take(cancellationToken); }
                    catch (OperationCanceledException) { break; }

                    g.MergeQueuedTxGraphs(cancellationToken);

                    var csvBuilder = new StringBuilder();

                    if (!File.Exists(filename))
                        csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                        { "Source", "Target", "Value", "EdgeType", "Timestamp" }));

                    foreach (var edge in g.Edges)
                        csvBuilder.AppendLine(
                            string.Join(_delimiter, new string[]
                            {
                                mapper.GetId(edge.Source).ToString(),
                                mapper.GetId(edge.Target).ToString(),
                                edge.Value.ToString(),
                                ((byte)edge.Type).ToString(),
                                edge.Timestamp.ToString()
                            }));

                    File.AppendAllText(filename, csvBuilder.ToString());
                }
            })
            {
                IsBackground = false
            };
            thread.Start();
        }

        public void Enqueue(GraphBase g)
        {
            _buffer.Add(g);
        }
    }
}
