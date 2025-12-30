using System.IO.Compression;

namespace EBA.Blockchains.Bitcoin.Utilities;

public class Deduplicator
{
    private static readonly ulong conssoleLogInterval = 1000000;

    public static async Task DedupScriptNodesFile(
        string inputSortedScriptNodesFile,
        string outputUniqueScriptNodesFile,
        ILogger<BitcoinOrchestrator> logger,
        CancellationToken ct)
    {
        using var reader = new StreamReader(
            inputSortedScriptNodesFile);

        outputUniqueScriptNodesFile += ".gz";
        using var writer = new StreamWriter(
            new GZipStream(
                File.Create(outputUniqueScriptNodesFile),
                CompressionMode.Compress));

        logger.LogInformation(
            "Processing script nodes de-duplication: input={in}, output={out}",
            inputSortedScriptNodesFile,
            outputUniqueScriptNodesFile);

        ct.ThrowIfCancellationRequested();

        var line = reader.ReadLine();
        if (line == null)
        {
            logger.LogError("Input script nodes file is empty: {f}", inputSortedScriptNodesFile);
            return;
        }

        var parts = line.Split('\t');
        var preID = parts[0];

        writer.WriteLine(line);

        ulong lineCounter = 1;
        while ((line = reader.ReadLine()) != null)
        {
            lineCounter++;
            if (lineCounter % conssoleLogInterval == 0)
            {
                logger.LogInformation("Processed {n:N0} script nodes.", lineCounter);
                writer.Flush();
            }

            parts = line.Split('\t');
            if (preID != parts[0])
            {
                writer.WriteLine(line);
                preID = parts[0];
            }

            if (ct.IsCancellationRequested)
            {
                logger.LogWarning("Script nodes de-duplication cancelled.");
                writer.Flush();
                writer.Close();
                ct.ThrowIfCancellationRequested();
            }
        }

        logger.LogInformation(
            "Script nodes de-duplication completed. Total processed lines: {n:N0}.",
            lineCounter);
    }


    public static async Task ProcessTxNodesFile(
        string inputSortedTxNodesFile,
        string outputUniqueTxNodesFile,
        ILogger<BitcoinOrchestrator> logger,
        CancellationToken ct)
    {
        using var reader = new StreamReader(
            inputSortedTxNodesFile);

        outputUniqueTxNodesFile += ".gz";
        using var writer = new StreamWriter(
            new GZipStream(
                File.Create(outputUniqueTxNodesFile),
                CompressionMode.Compress));

        logger.LogInformation(
            "Processing Tx nodes de-duplication: input={in}, output={out}",
            inputSortedTxNodesFile,
            outputUniqueTxNodesFile);

        ct.ThrowIfCancellationRequested();

        var line = reader.ReadLine();
        if (line == null)
        {
            logger.LogError("Input Tx nodes file is empty: {f}", inputSortedTxNodesFile);
            return;
        }

        var parts = line.Split('\t');
        var prevParts = parts;
        ulong lineCounter = 1;

        while ((line = reader.ReadLine()) != null)
        {
            lineCounter++;
            if (lineCounter % conssoleLogInterval == 0)
            {
                logger.LogInformation("Processed {n:N0} Tx nodes.", lineCounter);
                writer.Flush();
            }

            parts = line.Split('\t');
            if (prevParts[0] != parts[0])
            {
                writer.WriteLine(string.Join('\t', prevParts));
                prevParts = parts;
            }
            else
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(prevParts[i]))
                    {
                        prevParts[i] = parts[i];
                    }
                    else if (!string.IsNullOrWhiteSpace(parts[i]) && prevParts[i] != parts[i])
                    {
                        logger.LogWarning(
                            "Different values for the same tx node: {v1} vs {v2}",
                            prevParts[i],
                            parts[i]);
                    }
                }
            }

            if (ct.IsCancellationRequested)
            {
                logger.LogWarning("Tx nodes de-duplication cancelled.");
                writer.Flush();
                writer.Close();
                ct.ThrowIfCancellationRequested();
            }
        }

        writer.WriteLine(string.Join('\t', prevParts));

        logger.LogInformation(
            "Tx nodes de-duplication completed. Total processed lines: {n:N0}.",
            lineCounter);
    }
}
