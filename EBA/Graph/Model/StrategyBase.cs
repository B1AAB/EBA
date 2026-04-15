using System.IO.Compression;
using System.Linq.Expressions;

namespace EBA.Graph.Model;

public abstract class StrategyBase<TElement, TSchema> : IElementStrategy
    where TSchema : IElementSchema<TElement>
{
    public string DefaultFilename { get; }

    private string? _filename;
    private StreamWriter? _writer;
    private bool _disposed = false;
    private readonly bool _serializeCompressed;

    public const string csvDelimiter = "\t";

    public StrategyBase(string defaultFilename, bool serializeCompressed)
    {
        _serializeCompressed = serializeCompressed;
        DefaultFilename = defaultFilename + (_serializeCompressed ? ".csv.gz" : ".csv");
    }

    private StreamWriter GetStreamWriter(string filename)
    {
        if (_writer is null || _filename != filename)
        {
            _filename = filename;

            _writer?.Dispose();

            if (_serializeCompressed)
                _writer = new StreamWriter(new GZipStream(File.Create(_filename), compressionLevel: CompressionLevel.Optimal));
            else
                _writer = new StreamWriter(_filename);

            _writer.AutoFlush = true;

            return _writer;
        }
        else
        {
            return _writer;
        }
    }

    public async Task ToCsvAsync(IGraphElement element, string filename)
    {
        await GetStreamWriter(filename).WriteLineAsync(GetCsvRow(element));
    }

    public async Task ToCsvAsync<T>(IEnumerable<T> elements, string filename) where T : IGraphElement
    {
        await GetStreamWriter(filename).WriteLineAsync(
            string.Join(
                Environment.NewLine,
                from x in elements select GetCsvRow(x)));
    }

    public virtual string GetCsvHeader()
    {
        return TSchema.Mapper.GetCsvHeader();
    }

    public virtual string GetCsvRow(IGraphElement element)
    {
        return GetCsv((TElement)element);
    }

    public static string GetCsv(TElement element)
    {
        return TSchema.Mapper.GetCsv(element);
    }

    public virtual string GetQuery(string filename)
    {
        throw new NotImplementedException("GetQuery is not implemented.");
    }

    public virtual string[] GetSchemaConfigs()
    {
        return [];
    }

    public virtual string[] GetSeedingCommands()
    {
        return [];
    }

    public static Func<string[], T> GetFieldParser<T>(Expression<Func<TElement, T>> e)
    {
        var memberExpression = e.Body switch
        {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Expression must be a member access.")
        };

        var i = TSchema.Mapper.GetPropertyCsvIndex(memberExpression.Member.Name);

        return columns =>
        {
            return (T)Convert.ChangeType(columns[i], typeof(T));
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _writer?.Dispose();
            }

            _disposed = true;
        }
    }
}
