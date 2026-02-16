namespace EBA.Graph.Model;

public interface IGraphElement
{
    /// <summary>
    /// An identifier for the graph element, 
    /// used to distinguish between different types of elements.
    /// This identifier is instance-specific, 
    /// for instance, an edge of 
    /// "Tx -[Transfer]-> Tx" has a different kind than an edge of 
    /// "Tx -[Fee]-> Tx".
    /// </summary>
    string Kind { get; }
}
