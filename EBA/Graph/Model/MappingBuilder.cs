using EBA.Graph.Bitcoin.Strategies;
using EBA.Graph.Db.Neo4jDb;
using System.Linq.Expressions;

namespace EBA.Graph.Model;

public class MappingBuilder<T>
{
    private readonly List<PropertyMapping<T>> _mappings = [];

    public PropertyMapping<T>[] ToArray()
    {
        return [.. _mappings];
    }

    public MappingBuilder<T> Map<TProperty>(Expression<Func<T, TProperty>> e)
    {
        var eMember = e.Body switch
        {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Expression must be a member access.")
        };
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

    public MappingBuilder<T> MapBlockHeight(Func<T, long> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                PropertyMappingFactory.HeightProperty, 
                x => selector(x), 
                deserializer: v => (long)v!));
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

    public MappingBuilder<T> MapRange(IEnumerable<PropertyMapping<T>> mappings)
    {
        _mappings.AddRange(mappings);
        return this;
    }

    public MappingBuilder<T> MapCustom<TProperty>(string customColumnName, Func<T, TProperty> selector)
    {
        _mappings.Add(new PropertyMapping<T>(
            new Property(customColumnName, GetNeo4jType(typeof(TProperty))),
            x => selector(x)
        ));

        return this;
    }

    public MappingBuilder<T> MapLabel<TProperty>(Func<T, TProperty> selector)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                ":LABEL", 
                FieldType.String,
                x => selector(x),
                _ => ":LABEL"));

        return this;
    }

    public MappingBuilder<T> WithCsvHeader(Func<Property, string> headerOverride)
    {
        if (_mappings.Count == 0)
            throw new InvalidOperationException("Cannot call WithCsvHeader before adding a map.");

        var lastMapping = _mappings[^1];
        
        _mappings[^1] = new PropertyMapping<T>(
            lastMapping.Property,
            x => lastMapping.GetValue(x),
            headerOverride,
            null
        );

        return this;
    }

    private static FieldType GetNeo4jType(Type type)
    {
        // extracts underlying type if nullable
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType switch
        {
            { IsEnum: true } 
            => FieldType.String,

            _ when actualType == typeof(int) 
                || actualType == typeof(uint) 
            => FieldType.Int,

            _ when actualType == typeof(long) 
                || actualType == typeof(ulong) 
            => FieldType.Long,

            _ when actualType == typeof(float) 
            => FieldType.Float,

            _ when actualType == typeof(double) 
            => FieldType.Double,

            _ when actualType == typeof(bool) 
            => FieldType.Boolean,

            _ when actualType == typeof(string) 
            => FieldType.String,

            _ when actualType == typeof(string[]) 
            => FieldType.StringArray,

            _ when actualType == typeof(long[]) 
            => FieldType.LongArray,

            _ when actualType == typeof(double[]) 
            => FieldType.DoubleArray,

            _ => throw new ArgumentException($"Unsupported type for Neo4j FieldType: {type.Name}")
        };
    }
}