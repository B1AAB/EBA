namespace BC2G.Model
{
    internal class Status
    {
        public int StartBlock { get; set; }
        public int LastProcessedBlock { get; set; } = -1;
        public int EndBlock { get; set; }
    }
}
