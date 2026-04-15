namespace EBA.Graph.Model;

public interface IElementSchema<T>
{
    public static string IdSpace { get; }

    public static abstract EntityTypeMapper<T> Mapper { get; }
}
