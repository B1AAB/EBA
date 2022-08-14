using BC2G.DAL;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.CLI
{
    // TODO: read docs: https://docs.microsoft.com/en-us/dotnet/standard/commandline/model-binding
    // Does not this class seem a bit overkil?! 

    internal class OptionsBinder : BinderBase<Options>
    {
        private readonly Option<int>? _fromInclusiveOption;
        private readonly Option<int>? _toExclusiveOption;
        private readonly Option<int>? _granularityOption;
        private readonly Option<int>? _graphSampleCountOption;
        private readonly Option<int>? _graphSampleHopsOption;
        private readonly Option<int>? _graphSampleMinNodeCount;
        private readonly Option<int>? _graphSampleMaxNodeCount;
        private readonly Option<int>? _graphSampleMinEdgeCount;
        private readonly Option<int>? _graphSampleMaxEdgeCount;
        private readonly Option<GraphSampleMode>? _graphSampleModeOption;
        private readonly Option<string>? _workingDirOption;
        private readonly Option<string>? _statusFilenameOption;

        public OptionsBinder(

            Option<int>? fromInclusiveOption = null,
            Option<int>? toExclusiveOption = null,
            Option<int>? graphSampleCountOption = null,
            Option<int>? graphSampleHopOption = null,
            Option<int>? graphSampleMinNodeCount = null,
            Option<int>? graphSampleMaxNodeCount = null,
            Option<int>? graphSampleMinEdgeCount = null,
            Option<int>? graphSampleMaxEdgeCount = null,
            Option<GraphSampleMode>? graphSampleModeOption = null,
            Option<int>? granularityOption = null,
            Option<string>? workingDirOption = null,
            Option<string>? statusFilenameOption = null)
        {
            _fromInclusiveOption = fromInclusiveOption;
            _toExclusiveOption = toExclusiveOption;
            _granularityOption = granularityOption;
            _graphSampleCountOption = graphSampleCountOption;
            _graphSampleHopsOption = graphSampleHopOption;
            _graphSampleMinNodeCount = graphSampleMinNodeCount;
            _graphSampleMaxNodeCount = graphSampleMaxNodeCount;
            _graphSampleMinEdgeCount = graphSampleMinEdgeCount;
            _graphSampleMaxEdgeCount = graphSampleMaxEdgeCount;
            _graphSampleModeOption = graphSampleModeOption;
            _workingDirOption = workingDirOption;
            _statusFilenameOption = statusFilenameOption;
        }

        protected override Options GetBoundValue(BindingContext c)
        {
            var o = new Options();
            if (_fromInclusiveOption != null)
                o.FromInclusive = c.ParseResult.GetValueForOption(_fromInclusiveOption);

            if (_toExclusiveOption != null)
                o.ToExclusive = c.ParseResult.GetValueForOption(_toExclusiveOption);

            if (_granularityOption != null)
                o.Granularity = c.ParseResult.GetValueForOption(_granularityOption);

            if (_graphSampleCountOption != null)
                o.GraphSampleCount = c.ParseResult.GetValueForOption(_graphSampleCountOption);

            if (_graphSampleHopsOption != null)
                o.GraphSampleHops = c.ParseResult.GetValueForOption(_graphSampleHopsOption);

            if (_graphSampleMinNodeCount != null)
                o.GraphSampleMinNodeCount = c.ParseResult.GetValueForOption(_graphSampleMinNodeCount);

            if (_graphSampleMaxNodeCount != null)
                o.GraphSampleMaxNodeCount = c.ParseResult.GetValueForOption(_graphSampleMaxNodeCount);

            if (_graphSampleMinEdgeCount != null)
                o.GraphSampleMinEdgeCount = c.ParseResult.GetValueForOption(_graphSampleMinEdgeCount);

            if (_graphSampleMaxNodeCount != null)
                o.GraphSampleMaxEdgeCount = c.ParseResult.GetValueForOption(_graphSampleMaxEdgeCount);

            if (_graphSampleModeOption != null)
                o.GraphSampleMode = c.ParseResult.GetValueForOption(_graphSampleModeOption);

            if (_workingDirOption != null)
            {
                var wd = c.ParseResult.GetValueForOption(_workingDirOption);
                if (wd != null)
                    o.WorkingDir = wd;
            }

            if (_statusFilenameOption != null)
            {
                var sf = c.ParseResult.GetValueForOption(_statusFilenameOption);
                if (sf != null)
                    o.StatusFile = sf;
            }

            return o;
        }
    }
}
