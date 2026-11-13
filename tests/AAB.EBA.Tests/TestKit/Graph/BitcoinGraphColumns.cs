using System;

namespace AAB.EBA.Tests.TestKit.Graph;

public static class BitcoinGraphColumns
{
    public static bool ScriptTypeProfile(string type, string column) => type == "nodes_Block"
        && (column.StartsWith("InputsCount.ScriptType.", StringComparison.Ordinal)
            || column.StartsWith("OutputsCount.ScriptType.", StringComparison.Ordinal)
            || column.StartsWith("InputsValue.ScriptType.", StringComparison.Ordinal)
            || column.StartsWith("OutputsValue.ScriptType.", StringComparison.Ordinal));

    public static bool TxFeatures(string type, string column) => type == "nodes_Tx"
        && !column.Contains(":ID(", StringComparison.Ordinal) && column != ":LABEL";

    public static GraphCsvComparison Stable => new GraphCsvComparison()
        .IgnoringColumns(ScriptTypeProfile).IgnoringColumns(TxFeatures);
}
