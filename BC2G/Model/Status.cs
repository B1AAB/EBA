namespace BC2G.Model
{
    internal class Status
    {
        public int FromInclusive { get; set; }
        public int LastProcessedBlock { get; set; } = -1;
        public int ToExclusive { get; set; }
    }
}
