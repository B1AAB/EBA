namespace EBA.Graph.Model;

public interface IGraphCodec<TElement> : IDisposable
{
    string DefaultFilename { get; }
    string GetCsvHeader();

    Task WriteCsvAsync(TElement element, string filename);
    Task WriteCsvAsync(IEnumerable<TElement> elements, string filename);

    string[] GetSchemaConfigs();
    string[] GetSeedingCommands();
}
