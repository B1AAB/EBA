namespace BC2G.Serializers
{
    internal class AddressToIdMapper : Dictionary<string, int>
    {
        private readonly string _filename;
        private const string _delimiter = "\t";

        public AddressToIdMapper(string filename)
        {
            _filename = filename;
            if (!File.Exists(filename))
                File.Create(filename).Dispose();
            Load();
        }

        public int GetId(string address)
        {
            // This approach writes to the file everytime a key is not found, 
            // this is not ideal solution and should be very slow. 
            // TODO: improve on this.
            if (!TryGetValue(address, out int id))
            {
                id = Count;
                Add(address, id);
                WriteMapping(address, id);
            }

            return id;
        }

        private void Load()
        {
            using var reader = new StreamReader(_filename);
            string? line;
            string[] sLine;
            while ((line = reader.ReadLine()) != null)
            {
                sLine = line.Split(_delimiter);
                Add(sLine[1], int.Parse(sLine[0]));
            }
        }

        private void WriteMapping(string address, int id)
        {
            using var writer = new StreamWriter(_filename, append: true);
            writer.WriteLine($"{id}{_delimiter}{address}");
        }
    }
}
