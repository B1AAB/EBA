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

        protected override void ToConsole()
        {
            // Do not use tab (\t) since the length of each string
            // is used to determine how many blank spaces to add or
            // when to truncate the line w.r.t console window width.
            string[] msgs = new[]
            {
                $"\r    Active Blocks: {ActiveBlocks}",
                $"\r    Completed:     {Completed,9:n0}/{Total:n0} ({Percentage:f2}%)",
                $"\r    Block Rate:    {BlockRuntimeMovingAvg.Speed,9} blocks/sec",
                $"\r    Edge Rate:     {EdgeRuntimeMovingAvg.Speed,9} edges/sec",
                $"\r    Nodes:         {NodesCount,9:n0}",
                $"\r    Edges:         {EdgesCount,9:n0}"
            };
            AsyncConsole.WriteLines(msgs, _colors);
        }
    }
}
