﻿using BC2G.Utilities;

using System.Collections.Immutable;

namespace BC2G.Graph.Model;

public class GraphBase<T>(string? id = null) : IEquatable<GraphBase<T>>, IGraphComponent, IDisposable
    where T : IContext
{
    public string Id { get; } = id == null ?  Helpers.GetTimestamp() : id.Trim();

    private bool _disposed = false;

    public static GraphComponentType ComponentType { get { return GraphComponentType.Graph; } }
    public GraphComponentType GetGraphComponentType() => ComponentType;

    public int NodeCount
    {
        get { return (from x in _nodes select x.Value.Count).Sum(); }
    }
    public int EdgeCount
    {
        get { return (from x in _edges select x.Value.Count).Sum(); }
    }

    public ReadOnlyCollection<INode<T>> Nodes
    {
        get
        {
            return new ReadOnlyCollection<INode<T>>(
                _nodes.SelectMany(x => x.Value.Values).ToList());
        }
    }
    public ReadOnlyCollection<IEdge<INode<T>, INode<T>, T>> Edges
    {
        get
        {
            return new ReadOnlyCollection<IEdge<INode<T>, INode<T>, T>>(
                _edges.SelectMany(x => x.Value.Values).ToList());
        }
    }

    private readonly ConcurrentDictionary<GraphComponentType, ConcurrentDictionary<string, INode<T>>> _nodes = new();
    private readonly ConcurrentDictionary<GraphComponentType, ConcurrentDictionary<string, IEdge<INode<T>, INode<T>, T>>> _edges = new();

    public ReadOnlyDictionary<string, string> Labels
    {
        get { return new ReadOnlyDictionary<string, string>(_labels); }
    }
    private readonly Dictionary<string, string> _labels = [];

    public int GetNodeCount(GraphComponentType type)
    {
        if (_nodes.TryGetValue(type, out ConcurrentDictionary<string, INode<T>>? value))
            return value.Values.Count;
        return 0;
    }

    public ImmutableDictionary<GraphComponentType, ICollection<INode<T>>> GetNodes()
    {
        return _nodes.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public ImmutableDictionary<GraphComponentType, ICollection<IEdge<INode<T>, INode<T>, T>>> GetEdges()
    {
        return _edges.ToImmutableDictionary(x => x.Key, x => x.Value.Values);
    }

    public void GetNode(string id, out INode<T> node, out GraphComponentType graphComponentType)
    {
        foreach (var nodeTypes in _nodes)
        {
            nodeTypes.Value.TryGetValue(id, out var n);
            if (n != null)
            {
                node = n;
                graphComponentType = nodeTypes.Key;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public void GetEdge(string id, out IEdge<INode<T>, INode<T>, T> edge, out GraphComponentType graphComponentType)
    {
        foreach (var edgeTypes in _edges)
        {
            edgeTypes.Value.TryGetValue(id, out var e);
            if (e != null)
            {
                edge = e;
                graphComponentType = edgeTypes.Key;
                return;
            }
        }

        throw new NotImplementedException();
    }

    public bool TryAddNode<U>(GraphComponentType type, U node) where U : INode<T>
    {
        // TODO: this is a hotspot 
        var x = _nodes.GetOrAdd(
            type,
            new ConcurrentDictionary<string, INode<T>>());

        return x.TryAdd(node.Id, node);
    }

    public U GetOrAddNode<U>(GraphComponentType type, U node) where U : INode<T>
    {
        var x = _nodes.GetOrAdd(
            type,
            new ConcurrentDictionary<string, INode<T>>());

        return (U)x.AddOrUpdate(node.Id, node, (key, oldValue) => node);
        // TODO: any better update logic?!
    }

    public void AddNodes<U>(GraphComponentType type, IEnumerable<U> nodes) where U : INode<T>
    {
        foreach (var node in nodes)
            GetOrAddNode(type, node);
    }

    public U GetOrAddEdge<U>(GraphComponentType type, U edge) where U : IEdge<INode<T>, INode<T>, T>
    {
        var x = _edges.GetOrAdd(
            type,
            new ConcurrentDictionary<string, IEdge<INode<T>, INode<T>, T>>());

        return (U)x.GetOrAdd(edge.Id, edge);
    }

    public void AddEdges<U>(GraphComponentType type, IEnumerable<U> edges)
        where U : IEdge<INode<T>, INode<T>, T>
    {
        foreach (var edge in edges)
            GetOrAddEdge(type, edge);
    }

    public void AddOrUpdateEdge<U>(
        U edge, Func<string, IEdge<INode<T>, INode<T>, T>, IEdge<INode<T>, INode<T>, T>> updateValueFactory,
        GraphComponentType sourceType,
        GraphComponentType targetType,
        GraphComponentType edgeType)
        where U : IEdge<INode<T>, INode<T>, T>
    {
        var x = _edges.GetOrAdd(
            edgeType,
            new ConcurrentDictionary<string, IEdge<INode<T>, INode<T>, T>>());

        x.AddOrUpdate(edge.Id, edge, updateValueFactory);

        edge.Source.AddOutgoingEdge(edge);
        edge.Target.AddIncomingEdge(edge);

        TryAddNode(sourceType, edge.Source);
        TryAddNode(targetType, edge.Target);
    }

    public List<U>? GetEdges<U>(GraphComponentType type) where U : IEdge<INode<T>, INode<T>, T>
    {
        if (!_edges.ContainsKey(type))
            return null;

        return _edges[type].Cast<U>().ToList();
    }

    public void AddLabel(string key, string value)
    {
        _labels[key] = value;
    }

    public void Serialize(
        string workingDir, 
        string perBatchLabelsFilename, 
        bool serializeFeatureVectors = true, 
        bool serializeEdges = false)
    {
        Directory.CreateDirectory(workingDir);

        if (serializeFeatureVectors)
            SerializeFeatures(workingDir, perBatchLabelsFilename);

        if (serializeEdges)
            SerializeEdges(workingDir);
    }

    public void SerializeEdges(string workingDir, string edgesFilename = "edges.tsv")
    {
        var header = new[] { "SourceId", "TargetId", "SourceNodeType", "TargetNodeType", "EdgeValue", "EdgeType" };

        var edges = _edges.Values.SelectMany(ids => ids.Values).Select(
            edges => new[]
            {
                edges.Source.GetUniqueLabel(),
                edges.Target.GetUniqueLabel(),
                edges.Source.GetGraphComponentType().ToString(),
                edges.Target.GetGraphComponentType().ToString(),
                edges.Value.ToString(),
                edges.Type.ToString()
            });

        Helpers.CsvSerialize(edges, Path.Combine(workingDir, edgesFilename), header, append: true);
        Helpers.CsvSerialize(edges, Path.Combine(workingDir, edgesFilename), header);
    }

    public GraphFeatures<T> GetFeatures()
    {
        return new GraphFeatures<T>(this);
    }

    public void SerializeFeatures(
        string workingDir,
        string perBatchLabelsFilename,
        string perGraphLabelsFilename = "Labels.tsv")
    {
        var gFeatures = GetFeatures();

        foreach (var nodeType in gFeatures.NodeFeatures)
        {
            if (nodeType.Value.Count == 0)
                continue;

            Helpers.CsvSerialize(
                nodeType.Value,
                Path.Join(workingDir, nodeType.Key + ".tsv"),
                gFeatures.NodeFeaturesHeader[nodeType.Key]);
        }

        foreach (var edgeType in gFeatures.EdgeFeatures)
        {
            if (edgeType.Value.Count == 0)
                continue;

            Helpers.CsvSerialize(
                edgeType.Value,
                Path.Join(workingDir, edgeType.Key + ".tsv"),
                gFeatures.EdgeFeaturesHeader[edgeType.Key]);
        }

        Helpers.CsvSerialize(
            [gFeatures.Labels.ToArray()],
            Path.Combine(workingDir, perBatchLabelsFilename),
            gFeatures.LabelsHeader,
            append: true);

        Helpers.CsvSerialize(
            [gFeatures.Labels.ToArray()],
            Path.Combine(workingDir, perGraphLabelsFilename),
            gFeatures.LabelsHeader);
    }


    public void DownSample(int maxNodesCount, int maxEdgesCount, int? seed = null)
    {
        // TODO: this sampling is not ideal;
        // (1) it is not the fastest;
        // (2) it may lead to having fewer nodes/edges than requested;
        // (3) most importantly, it removes nodes/edges independent of
        //     "path" so it may lead to turning a graph into more than one subgraphs.
        //
        // For a better sampling algorithm, use "Reservoir sampling". Ref: 
        // - https://stackoverflow.com/a/48089/947889
        // - https://en.wikipedia.org/wiki/Reservoir_sampling
        //

        Random rnd = seed == null ? new Random() : new Random((int)seed);
        var nodesToRemoveCount = NodeCount - maxNodesCount;
        if (nodesToRemoveCount > 0)
        {
            var allNodesIds = _nodes.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var nodesToRemove = allNodesIds.OrderBy(x => rnd.Next()).Take(nodesToRemoveCount);

            foreach (var nodeToRemove in nodesToRemove)
            {
                _nodes[(GraphComponentType)nodeToRemove[0]].Remove((string)nodeToRemove[1], out var removedNode);

                if (removedNode != null)
                {
                    foreach (var e in removedNode.IncomingEdges)
                        _edges[e.GetGraphComponentType()].Remove(e.Id, out _);

                    foreach (var e in removedNode.OutgoingEdges)
                        _edges[e.GetGraphComponentType()].Remove(e.Id, out _);
                }
            }
        }

        var edgesToRemoveCount = EdgeCount - maxEdgesCount;
        if (edgesToRemoveCount > 0)
        {
            var allEdgesIds = _edges.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var edgesToRemove = allEdgesIds.OrderBy(x => rnd.Next()).Take(edgesToRemoveCount);

            foreach (var edgeToRemove in edgesToRemove)
                _edges[(GraphComponentType)edgeToRemove[0]].Remove((string)edgeToRemove[1], out _);
        }

        var disconnectNodes = _nodes
            .SelectMany(t => t.Value.Where(n => n.Value.InDegree == 0 & n.Value.OutDegree == 0)
            .Select(x => new object[] { t.Key, x.Key }));

        foreach (var node in disconnectNodes)
            _nodes[(GraphComponentType)node[0]].Remove((string)node[1], out _);
    }

    public void DownSample(int maxEdgesCount, int? seed = null)
    {
        // TODO: this sampling is not ideal
        // because it can be very slow
        //
        // For a better sampling algorithm, use "Reservoir sampling". Ref: 
        // - https://stackoverflow.com/a/48089/947889
        // - https://en.wikipedia.org/wiki/Reservoir_sampling
        //

        Random rnd = seed == null ? new Random() : new Random((int)seed);
        var edgesToRemoveCount = EdgeCount - maxEdgesCount;

        if (edgesToRemoveCount > 0)
        {
            var removedEdgesCounter = 0;
            foreach(var edgeType in _edges)
            {
                foreach(var edge in edgeType.Value)
                {

                }
            }

            var allEdgesIds = _edges.SelectMany(x => x.Value.Select(y => new object[] { x.Key, y.Key })).ToList();
            var edgesToRemove = allEdgesIds.OrderBy(x => rnd.Next()).Take(edgesToRemoveCount);

            foreach (var edgeToRemove in edgesToRemove)
                _edges[(GraphComponentType)edgeToRemove[0]].Remove((string)edgeToRemove[1], out _);
        }
    }

    public bool Equals(GraphBase<T>? other)
    {
        if (other == null)
            return false;

        return ReferenceEquals(this, other);
    }
    public override bool Equals(object? other)
    {
        return Equals(other as GraphBase<T>);
    }
    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            { }
        }

        _disposed = true;
    }
}
