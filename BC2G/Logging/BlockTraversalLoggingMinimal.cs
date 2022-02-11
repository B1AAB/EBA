namespace BC2G.Logging
{
    internal class BlockTraversalLoggingMinimal : BlockTraversalLoggingBase
    {
        public BlockTraversalLoggingMinimal(int fromInclusive, int toExclusive) :
            base(fromInclusive, toExclusive, 0)
        { }

        protected override void ToConsole()
        {
            AsyncConsole.Write(
                $"\r\tCompleted:\t{Completed:n0}/{Total:n0} ({Percentage:f2}%)" +
                $"\tCB:\t{ActiveBlocksCount}" +
                $"\tNodes:\t{NodesCount:n0}" +
                $"\tEdges:\t{EdgesCount:n0}",
                ConsoleColor.Cyan);
        }
    }
}
