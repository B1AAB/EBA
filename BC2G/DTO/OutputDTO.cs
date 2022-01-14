namespace BC2G.DTO
{
    public class OutputDTO
    {
        public string Address { get; }
        public double Value { get; }

        public OutputDTO(string address, double value)
        {
            Address = address;
            Value = value;
        }
    }
}
