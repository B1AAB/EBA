using EBA.Graph.Db.Neo4jDb;
using System.Linq.Expressions;

namespace EBA.Graph.Bitcoin.Strategies;

public class MappingBuilder<T>
{
    private readonly List<PropertyMapping<T>> _mappings = [];

    public PropertyMapping<T>[] ToArray()
    {
        return [.. _mappings];
    }

    public MappingBuilder<T> Map<TProperty>(Expression<Func<T, TProperty>> e)
    {
        var eMember = e.Body as MemberExpression;
        if (eMember == null && e.Body is UnaryExpression unaryExpression)
            eMember = unaryExpression.Operand as MemberExpression;

        if (eMember == null)
            throw new ArgumentException("Expression must be a member access.");

        var compiledFunc = e.Compile();

        _mappings.Add(new PropertyMapping<T>(
            new Property(
                eMember.Member.Name, 
                GetNeo4jType(typeof(TProperty))),
            x => compiledFunc(x)
        ));

        return this;
    }

    public MappingBuilder<T> MapSourceId<TProperty>(
        string idSpace, 
        Func<T, TProperty> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                ":START_ID", 
                GetNeo4jType(typeof(TProperty)), 
                x => selector(x),
                _ => $":START_ID({idSpace})"));

        return this;
    }

    public MappingBuilder<T> MapTargetId<TProperty>(
        string idSpace, 
        Func<T, TProperty> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                ":END_ID", 
                GetNeo4jType(typeof(TProperty)), 
                x => selector(x), 
                _ => $":END_ID({idSpace})"));

        return this;
    }

    public MappingBuilder<T> MapEdgeType<TProperty>(Func<T, TProperty> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                PropertyMappingFactory.TypePropertyName, 
                FieldType.String,
                x => selector(x),
                _ => PropertyMappingFactory.TypePropertyName));

        return this;
    }

    public MappingBuilder<T> MapValue(Func<T, long> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                PropertyMappingFactory.ValueProperty, 
                x => selector(x), 
                deserializer: v => (long)v!));

        return this;
    }

    private static FieldType GetNeo4jType(Type type)
    {
        if (type == typeof(int) || type == typeof(int?)) 
            return FieldType.Int;
        if (type == typeof(long) || type == typeof(long?)) 
            return FieldType.Long;
        if (type == typeof(float) || type == typeof(float?)) 
            return FieldType.Float;
        if (type == typeof(double) || type == typeof(double?)) 
            return FieldType.Double;
        if (type == typeof(bool) || type == typeof(bool?)) 
            return FieldType.Boolean;
        if (type == typeof(string)) 
            return FieldType.String;
        if (type == typeof(string[])) 
            return FieldType.StringArray;
        if (type == typeof(long[])) 
            return FieldType.LongArray;
        if (type == typeof(double[])) 
            return FieldType.DoubleArray;

        throw new ArgumentException($"Unsupported type for Neo4j FieldType: {type.Name}");
    }
}