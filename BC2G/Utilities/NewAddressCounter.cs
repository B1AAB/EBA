namespace BC2G.Utilities.Utilities;

internal class NewAddressCounter
{
    public void Analyze(string inFilename, string outFilename)
    {
        var addresses = new HashSet<string>();

        const int BufferSize = 4096;
        using var inFileStream = File.OpenRead(inFilename);
        using var outFileStream = File.OpenWrite(outFilename);

        using var streamReader = new StreamReader(inFileStream, Encoding.UTF8, true, BufferSize);
        using var streamWriter = new StreamWriter(outFileStream, Encoding.UTF8);
        streamWriter.WriteLine(string.Join('\t', ["Block", "#AddressesInBlock", "#UniqueAddressesInBlock", "#UniqueAddresses"]));

        string? line;
        line = streamReader.ReadLine(); // Skip header        

        while ((line = streamReader.ReadLine()) != null)
        {
            var cols = line.Split('\t');
            var blockAddresses = cols[1].Split(';');
            var newAddressesCounter = 0;

            var blockSpecificAddresses = new HashSet<string>();

            foreach (var address in blockAddresses)
            {
                if (!addresses.Contains(address))
                {
                    newAddressesCounter++;
                    addresses.Add(address);
                }

                if (!blockSpecificAddresses.Contains(address))
                    blockSpecificAddresses.Add(address);
            }

            streamWriter.WriteLine(string.Join('\t', [cols[0], blockAddresses.Length, blockSpecificAddresses.Count, newAddressesCounter]));
        }
    }
}
