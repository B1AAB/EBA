using EBA.Blockchains.Bitcoin.GraphModel;
using Neo4j.Driver;

namespace AAB.EBA.MCP;

public static class EdgeSortingExtensions
{
    /// <summary>
    /// Sorts a mixed collection of Neo4j edges chronologically. 
    /// Uses CreationHeight for T2S edges and SpentHeight for S2T edges.
    /// </summary>
    public static List<IRelationship> SortByRelevantHeight(this IEnumerable<IRelationship> edges)
    {
        // Cache the relation types outside the loop for performance
        string t2sRelation = T2SEdge.Kind.Relation.ToString();
        string s2tRelation = S2TEdge.Kind.Relation.ToString();

        return edges.OrderBy(edge =>
        {
            if (edge.Type == t2sRelation)
            {
                return edge.Properties[nameof(T2SEdge.CreationHeight)].As<long>();
            }

            if (edge.Type == s2tRelation)
            {
                return edge.Properties[nameof(S2TEdge.SpentHeight)].As<long>();
            }

            // Fallback for any unknown edge types to push them to the end
            return long.MaxValue;
        }).ToList();
    }
}
