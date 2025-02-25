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
                "Block", "AddressesInBlockCount", "UniqueAddressesInBlockCount", "UniqueAddressesCount");
        }

        public static string ToStringHeaderWithoutHeight()
        {
            return string.Join(
                _delimiter,
                "AddressesInBlockCount", "UniqueAddressesInBlockCount", "UniqueAddressesCount");
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

    public void Analyze(string addressesFilename, string statsFilename, string workingDir, CancellationToken ct)
    {
        var tmpFilename = Path.Join(workingDir, $".tmp_{Path.GetRandomFileName()}");
        ExtractAddressStats(addressesFilename, tmpFilename, ct);

        var outFilename = Path.Join(
            Path.GetDirectoryName(statsFilename),
            $"{Path.GetFileNameWithoutExtension(statsFilename)}_extended{Path.GetExtension(statsFilename)}");

        if (ct.IsCancellationRequested)
            return;

        AddExtractedAddressStatsToStatsFile(workingDir, statsFilename, tmpFilename, outFilename, ct);
    }

    private void ExtractAddressStats(string addressesFilename, string outFilename, CancellationToken ct)
    {
        var addresses = new HashSet<string>();
        //var blocks = new Dictionary<string, string>();
        //var blocks = new Dictionary<string, string>();
        var blocks = new HashSet<string>();

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
            if (ct.IsCancellationRequested)
                return;

            lineCounter++;
            if (lineCounter % 1000 == 0)
                _logger.LogInformation("Read {counter} Lines.", lineCounter.ToString("N0"));

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
            // 
            // --- the following implementation handles duplicates, but it requires a lot of memory,
            // --- hence the second method is implemented that is a simplified implications with minimal
            // --- memory usage, but it will require manual intervention if duplicates is found.

            /* 
            Array.Sort(blockAddresses);
            var joinedSortedAddresses = string.Join(';', blockAddresses);
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(joinedSortedAddresses));
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
            */
            // the following is a simplified alternative to the above.
            if (!blocks.Add(blockHeight))
            {
                _logger.LogWarning(
                    "Duplicate block {b} found, manually check the files if the duplicated " +
                    "entries have the same addresses. This process considers the addresses in the first entry.", blockHeight);
                continue;
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

            if (ct.IsCancellationRequested)
                return;

            streamWriter.WriteLine(Stats.ToString(blockHeight, blockAddresses.Length, blockSpecificAddresses.Count, newAddressesCounter));
        }

        _logger.LogInformation("Finished extracting address stats, and persisting them in a temp file ({tmpFilename}).", outFilename);
    }

    private void AddExtractedAddressStatsToStatsFile(string workingDir, string statsFilename, string addressesStats, string outFilename, CancellationToken ct)
    {
        _logger.LogInformation("Started reading block stats and address stats, and extending block stats file with address stats.");

        using var addressesFileStream = File.OpenRead(addressesStats);
        using var addressesStreamReader = new StreamReader(addressesFileStream, Encoding.UTF8, true);

        string? line;
        _ = addressesStreamReader.ReadLine(); // Skip header

        var stats = new Dictionary<int, Stats>();

        if (ct.IsCancellationRequested)
            return;

        while ((line = addressesStreamReader.ReadLine()) != null)
        {
            var stat = Stats.Parse(line);
            stats.Add(stat.BlockHeight, stat);
        }

        if (ct.IsCancellationRequested)
            return;

        using var statsFileStream = File.OpenRead(statsFilename);
        using var statsStreamReader = new StreamReader(statsFileStream, Encoding.UTF8, true);

        using var streamWriter = new StreamWriter(outFilename, false, Encoding.UTF8);

        var header = statsStreamReader.ReadLine();
        streamWriter.WriteLine(header + '\t' + Stats.ToStringHeaderWithoutHeight());

        while ((line = statsStreamReader.ReadLine()) != null)
        {
            if (ct.IsCancellationRequested)
                return;

            line = line.TrimEnd('\r', '\n', '\t');

            var blockHeight = int.Parse(line.Split('\t')[0]);
            var stat = stats[blockHeight];
            streamWriter.WriteLine($"{line}\t{stat.ToStringWithoutHeight()}");
        }

        _logger.LogInformation("Finished adding address stats to block stats; filename: {outfile}", outFilename);
    }
}
