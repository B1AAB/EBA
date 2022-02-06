using BC2G.Graph;
using BC2G.Model;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G.Serializers
{
    public class BlocksStatisticsSerializer
    {
        public static void Serialize(
            ConcurrentQueue<GraphStatistics> blocksStats,
            string filename)
        {
            var strBuilder = new StringBuilder();

            if (!File.Exists(filename))
                strBuilder.AppendLine(string.Join("\t", new string[]
                {
                    "BlockHeight", "Runtime",
                    string.Join("\t", (EdgeType[]) Enum.GetValues(typeof(EdgeType)))
                }));

            foreach (var x in blocksStats)
                strBuilder.AppendLine(x.ToString());

            File.AppendAllText(filename, strBuilder.ToString());
        }
    }
}
