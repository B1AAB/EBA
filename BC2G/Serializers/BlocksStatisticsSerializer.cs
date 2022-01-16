using BC2G.Model;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G.Serializers
{
    public class BlocksStatisticsSerializer
    {
        public static void Serialize(
            ConcurrentQueue<BlockStatistics> blocksStats,
            string filename)
        {
            var strBuilder = new StringBuilder();

            strBuilder.Append(string.Join("\t", new string[]
            {
                "BlockHeight", "Runtime",
                string.Join("\t", (string[]) Enum.GetValues(typeof(EdgeType)))
            }));

            foreach (var x in blocksStats)
                strBuilder.AppendLine(x.ToString());

            File.WriteAllText(filename, strBuilder.ToString());
        }
    }
}
