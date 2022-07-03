using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.DAL.Bulkload
{
    public enum Prop
    {
        Height,
        ScriptAddress,
        ScriptType,
        BlockMedianTime,
        BlockConfirmations,
        BlockDifficulty,
        BlockTxCount,
        BlockSize,
        BlockStrippedSize,
        BlockWeight,
        NumGenerationEdges,
        NumTransferEdges,
        NumChangeEdges,
        NumFeeEdges,
        SumGenerationEdges,
        SumTransferEdges,
        SumChangeEdges,
        SumFeeEdges,
        EdgeSourceAddress,
        EdgeSourceType,
        EdgeTargetAddress,
        EdgeTargetType,
        EdgeType,
        EdgeValue
    }

    public enum FieldType
    {
        String, Int, Float
    }
}
