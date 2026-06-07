namespace AAB.EBA.TestDataGenerator;

public static class Dirs
{
    public static string Tests { get { return "tests"; } }

    public static string MockData { get { return "MockData"; } }

    public static string Bitcoin { get { return "Bitcoin"; } }

    public static string BitcoinCoreResponses { get { return "BitcoinCoreResponses"; } }

    public static string ExpectedOutputs { get { return "ExpectedOutputs"; } }

    public static string GetExpectedOutputDir(int from, int to)
    {
        return $"{from}-{to}";
    }
}

public static class Files
{
    public static string ChainInfo { get { return "chaininfo.json"; } }

    public static string HeightToHashMapFilename { get { return "height_to_hash_mapping.json"; } }

    public static string GetMockTxFilename(string txid)
    {
        return $"tx_{txid[..8]}.json";
    }

    public static string GetMockBlockFilename(int blockHeight)
    {
        return $"bl_{blockHeight}.json";
    }

    public static string GetMockBlockFilename(string hash)
    {
        return $"bl_{hash[^8..]}.json";
    }

    public static string[] GetAllMockBlockFilenames(string path)
    {
        return Directory.GetFiles(path, "bl_*.json");
    }
}
