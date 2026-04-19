using EBA.Graph.Db.Neo4jDb;
using System.Linq.Expressions;

namespace EBA.Graph.Model;

public class MappingBuilder
{
    public const string StartIdPropertyName = ":START_ID";
    public const string EndIdPropertyName = ":END_ID";
    public const string EdgeTypePropertyName = ":TYPE";
    public const string NodeLabelPropertyName = ":LABEL";

    public static string GetPropertyName(LambdaExpression expression)
    {
        var memberExpression = expression.Body switch
        {
            MemberExpression m => m,
            UnaryExpression { Operand: MemberExpression m } => m,
            _ => throw new ArgumentException("Expression must be a member access.")
        };

        return memberExpression.Member.Name;
    }

    public static FieldType ToFieldType(Type type)
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

            _ => throw new ArgumentException($"Unsupported type for: {type.Name}")
        };
    }
}

public class MappingBuilder<T>
{
    private readonly List<PropertyMapping<T>> _mappings = [];

    public PropertyMapping<T>[] ToArray()
    {
        return [.. _mappings];
    }

    public MappingBuilder<T> Map<TProperty>(Expression<Func<T, TProperty>> e)
    {
        _mappings.Add(new PropertyMapping<T>(
            new Property(
                MappingBuilder.GetPropertyName(e),
                MappingBuilder.ToFieldType(typeof(TProperty))),
            x => e.Compile()(x)
        ));

        return this;
    }

    public MappingBuilder<T> MapSourceId<TProperty>(
        string idSpace, 
        Func<T, TProperty> selector, 
        string propName = MappingBuilder.StartIdPropertyName)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                propName,
                MappingBuilder.ToFieldType(typeof(TProperty)), 
                x => selector(x),
                _ => $":START_ID({idSpace})"));

        return this;
    }

    public MappingBuilder<T> MapTargetId<TProperty>(
        string idSpace, 
        Func<T, TProperty> selector,
        string propName = MappingBuilder.EndIdPropertyName)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                propName,
                MappingBuilder.ToFieldType(typeof(TProperty)), 
                x => selector(x), 
                _ => $":END_ID({idSpace})"));

        return this;
    }

    public MappingBuilder<T> MapEdgeType<TProperty>(
        Func<T, TProperty> selector, 
        string propName = MappingBuilder.EdgeTypePropertyName)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                propName,
                FieldType.String,
                x => selector(x),
                _ => ":TYPE"));

        return this;
    }

    public MappingBuilder<T> Map(PropertyMapping<T> mapping)
    {
        _mappings.Add(mapping);
        return this;
    }

    public MappingBuilder<T> MapRange(IEnumerable<PropertyMapping<T>> mappings)
    {
        _mappings.AddRange(mappings);
        return this;
    }

    public MappingBuilder<T> Map<TProperty>(string name, Func<T, TProperty> selector)
    {
        _mappings.Add(new PropertyMapping<T>(
            new Property(name, MappingBuilder.ToFieldType(typeof(TProperty))),
            x => selector(x)
        ));

        return this;
    }

    public MappingBuilder<T> MapLabel<TProperty>(
        Func<T, TProperty> selector, 
        string propName = MappingBuilder.NodeLabelPropertyName)
    {
        _mappings.Add(
            new PropertyMapping<T>(
                propName, 
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

    internal PropertyMapping<T> GetLastMapping()
    {
        if (_mappings.Count == 0) 
            throw new InvalidOperationException("No mappings defined.");

        return _mappings[^1];
    }
}