namespace EBA.Graph.Model;

public interface IElementStrategy : IDisposable
{
    public string DefaultFilename { get; }
    public string[] GetSchemaConfigs();
    public string[] GetSeedingCommands();
    public abstract string GetCsvHeader();

    public Task ToCsvAsync(IGraphElement element, string filename);
    public Task ToCsvAsync<T>(IEnumerable<T> elements, string filename) where T : IGraphElement;
}
