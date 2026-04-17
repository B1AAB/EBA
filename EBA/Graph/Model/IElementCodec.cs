namespace EBA.Graph.Model;

public interface IElementCodec : IDisposable
{
    public string DefaultFilename { get; }
    public string[] GetSchemaConfigs();
    public string[] GetSeedingCommands();
    public abstract string GetCsvHeader();

    public Task WriteCsvAsync(IGraphElement element, string filename);
    public Task WriteCsvAsync<E>(IEnumerable<E> elements, string filename) where E : IGraphElement;
}
