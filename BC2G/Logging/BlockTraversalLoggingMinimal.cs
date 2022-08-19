namespace BC2G.Logging
{
    internal class BlockTraversalLoggingMinimal : BlockTraversalLoggingBase
    {
        public BlockTraversalLoggingMinimal(int fromInclusive, int toExclusive, int blocksCount) :
            base(fromInclusive, toExclusive, blocksCount, 0)
        { }

        protected override void ToConsole()
        {
            AsyncConsole.Write(
                $"\r   Completed:{$"{Completed:n0}",12}/{$"{Total:n0}"} ({$"{Percentage:f2}%",2})",
                ConsoleColor.Cyan);
        }
    }
}
