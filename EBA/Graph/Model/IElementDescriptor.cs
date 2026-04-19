namespace EBA.Graph.Model;

public interface IElementDescriptor<T>
{
    /// <summary>
    /// The namespace used to resolve IDs during import by Neo4j. 
    /// It should be unique across all node and edge types in the graph.
    /// </summary>
    static abstract string IdSpace { get; }

    ElementMapper<T> Mapper { get; }
    static abstract ElementMapper<T> StaticMapper { get; }

    virtual string[] UniqueProps => [];
    virtual string[]? Neo4jSchemaOverride => null;
    virtual string[]? Neo4jSeedingOverride => null;
}
