using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G.Utilities.Utilities;

internal class NewAddressCounter(ILogger logger)
{
    private readonly ILogger _logger = logger;

    private class Stats
    {
        public int BlockHeight { set; get; }
        public int AddressesInBlockCount { get; set; }
        public int UniqueAddressesInBlockCount { set; get; }
        public int UniqueAddressesCount { set; get; }

        private const char _delimiter = '\t';

        public static string ToStringHeader()
        {
            return string.Join(
                _delimiter,
                "Block", "#AddressesInBlock", "#UniqueAddressesInBlock", "#UniqueAddresses");
        }

        public static string ToStringHeaderWithoutHeight()
        {
            return string.Join(
                _delimiter,
                "#AddressesInBlock", "#UniqueAddressesInBlock", "#UniqueAddresses");
        }

        public static Stats Parse(string line)
        {
            var cols = line.Split(_delimiter);
            return new Stats()
            {
                BlockHeight = int.Parse(cols[0]),
                AddressesInBlockCount = int.Parse(cols[1]),
                UniqueAddressesInBlockCount = int.Parse(cols[2]),
                UniqueAddressesCount = int.Parse(cols[3])
            };
        }

        public static string ToString(string blockHeight, int addressInBlockCount, int uniqueAddressesInBlockCount, int uniqueAddressCount)
        {
            return string.Join(
                _delimiter,
                blockHeight, addressInBlockCount, uniqueAddressesInBlockCount, uniqueAddressCount);
        }

        public string ToStringWithoutHeight()
        {
            return string.Join(
                _delimiter,
                AddressesInBlockCount, UniqueAddressesInBlockCount, UniqueAddressesCount);
        }

        public override string ToString()
        {
            return string.Join(
                _delimiter,
                BlockHeight, AddressesInBlockCount, UniqueAddressesInBlockCount, UniqueAddressesCount);
        }
    }

    public void Analyze(string addressesFilename, string statsFilename, string workingDir)
    {
        var tmpFilename = Path.Join(workingDir, $".tmp_{Path.GetRandomFileName()}");
        ExtractAddressStats(addressesFilename, tmpFilename);

        var outFilename = Path.Join(
            Path.GetDirectoryName(statsFilename),
            $"{Path.GetFileNameWithoutExtension(statsFilename)}_extended{Path.GetExtension(statsFilename)}");

        AddExtractedAddressStatsToStatsFile(workingDir, statsFilename, tmpFilename, outFilename);
    }

    private void ExtractAddressStats(string addressesFilename, string outFilename)
    {
        var addresses = new HashSet<string>();
        var blocks = new Dictionary<string, string>();

        const int BufferSize = 4096;
        using var inFileStream = File.OpenRead(addressesFilename);
        using var outFileStream = File.OpenWrite(outFilename);

        using var streamReader = new StreamReader(inFileStream, Encoding.UTF8, true, BufferSize);
        using var streamWriter = new StreamWriter(outFileStream, Encoding.UTF8);
        streamWriter.WriteLine(Stats.ToStringHeader());

        string? line;
        line = streamReader.ReadLine(); // Skip header

        var lineCounter = 0;

        while ((line = streamReader.ReadLine()) != null)
        {
            lineCounter++;
            if (lineCounter % 1000 == 0)
                _logger.LogInformation("Read {counter} Lines.", lineCounter);

            var cols = line.Split('\t');
            var blockHeight = cols[0];
            var blockAddresses = cols[1].Split(';');
            var newAddressesCounter = 0;

            // This section is trying to make sure duplicate address stats are not included.
            // This can happen if the addressess file contains duplicates.
            // Duplicates may exist due to a bug in a corner case when traversing bitcoin,
            // when the processes is stopped and resumed and the processed block addresses
            // is not removed for the staged list.
            // TODO: fix this.
            Array.Sort(blockAddresses);
            var joinedSortedAddresses = string.Join(';', blockAddresses);
            var byteArray = Encoding.UTF8.GetBytes(joinedSortedAddresses);
            var hashBytes = SHA256.HashData(byteArray);
            var hashString = new StringBuilder();
            foreach (byte b in hashBytes)
                hashString.Append(b.ToString("x2"));
            var addressessHash = hashString.ToString();
            if (!blocks.TryAdd(blockHeight, addressessHash))
            {
                var previousHash = blocks[blockHeight];
                if (previousHash == addressessHash)
                {
                    _logger.LogWarning(
                        "Duplicate block addressess found, skipping duplicate entry. Height: {height}, Addresses hash: {hash}",
                        blockHeight,
                        addressessHash);
                    continue;
                }
                else
                {
                    _logger.LogWarning(
                        "Duplicate block addressess found, addresses hash do NOT match, adding duplicate entry. Height: {height}, Addresses hash: {hash}, Previous addresses hash: {pre}",
                        blockHeight,
                        addressessHash,
                        previousHash);
                }
            }

            var blockSpecificAddresses = new HashSet<string>();

            foreach (var address in blockAddresses)
            {
                if (!addresses.Contains(address))
                {
                    newAddressesCounter++;
                    addresses.Add(address);
                }

                blockSpecificAddresses.Add(address);
            }

            streamWriter.WriteLine(Stats.ToString(blockHeight, blockAddresses.Length, blockSpecificAddresses.Count, newAddressesCounter));
        }

        _logger.LogInformation("Finished extracting address stats, and persisting them in a temp file ({tmpFilename}).", outFilename);
    }

    private void AddExtractedAddressStatsToStatsFile(string workingDir, string statsFilename, string addressesStats, string outFilename)
    {
        _logger.LogInformation("Started reading block stats and address stats, and extending block stats file with address stats.");

        using var addressesFileStream = File.OpenRead(addressesStats);
        using var addressesStreamReader = new StreamReader(addressesFileStream, Encoding.UTF8, true);

        string? line;
        _ = addressesStreamReader.ReadLine(); // Skip header

        var stats = new Dictionary<int, Stats>();

        while ((line = addressesStreamReader.ReadLine()) != null)
        {
            var stat = Stats.Parse(line);
            stats.Add(stat.BlockHeight, stat);
        }

        using var statsFileStream = File.OpenRead(statsFilename);
        using var statsStreamReader = new StreamReader(statsFileStream, Encoding.UTF8, true);

        using var streamWriter = new StreamWriter(outFilename, false, Encoding.UTF8);

        var header = statsStreamReader.ReadLine();
        streamWriter.WriteLine(header + '\t' + Stats.ToStringHeaderWithoutHeight());

        while ((line = statsStreamReader.ReadLine()) != null)
        {
            var blockHeight = int.Parse(line.Split('\t')[0]);
            var stat = stats[blockHeight];
            streamWriter.WriteLine($"{line}\t{stat.ToStringWithoutHeight()}");
        }

        _logger.LogInformation("Finished adding address stats to block stats; filename: {outfile}", outFilename);
    }
}
