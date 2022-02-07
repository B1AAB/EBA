namespace BC2G.Logging
{
    public class ConsoleLoggingComplete : ConsoleLoggingBase
    {
        private static readonly ConsoleColor[] _colors = new[]
        {
            ConsoleColor.Cyan,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
        };

        public ConsoleLoggingComplete(int fromInclusive, int toExclusive) :
            base(fromInclusive, toExclusive, 6)
        { }

        public override void Log(int height)
        {
            base.Log(height);
            Log();
        }

        public override void Log(
            int height, 
            int allNodesCount, 
            int addedEdgesCount, 
            double runtime)
        {
            base.Log(height, allNodesCount, addedEdgesCount, runtime);
            Log();
        }

        private void Log()
        {
            // Do not use tab (\t) since the length of each string
            // is used to determine how many blank spaces to add or
            // when to truncate the line w.r.t console window width.
            string[] msgs = new[]
            {
                $"\r    Active Blocks: {ActiveBlocks}",
                $"\r    Completed:     {Completed,10:n0}/{Total:n0} ({Percentage:f2}%)",
                $"\r    Block Rage:    {BlockRuntimeMovingAvg.Speed,10:n0} bps",
                $"\r    Edge Rage:     {EdgeRuntimeMovingAvg.Speed,10:n0} eps",
                $"\r    Nodes:         {NodesCount,10:n0}",
                $"\r    Edges:         {EdgesCount,10:n0}"
            };
            AsyncConsole.WriteLines(msgs, _colors);
        }
    }
}
