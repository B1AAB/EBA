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
            o.FromInclusive = GetValue(o.FromInclusive, _fromInclusiveOption, c);
            o.ToExclusive = GetValue(o.ToExclusive, _toExclusiveOption, c);
            o.Granularity = GetValue(o.Granularity, _granularityOption, c);

            o.GraphSampleCount = GetValue(o.GraphSampleCount, _graphSampleCountOption, c);
            o.GraphSampleHops = GetValue(o.GraphSampleHops, _graphSampleHopsOption, c);
            o.GraphSampleMinNodeCount = GetValue(o.GraphSampleMinNodeCount, _graphSampleMinNodeCount, c);
            o.GraphSampleMaxNodeCount = GetValue(o.GraphSampleMaxNodeCount, _graphSampleMaxNodeCount, c);
            o.GraphSampleMinEdgeCount = GetValue(o.GraphSampleMinEdgeCount, _graphSampleMinEdgeCount, c);
            o.GraphSampleMaxEdgeCount = GetValue(o.GraphSampleMaxEdgeCount, _graphSampleMaxEdgeCount, c);
            o.GraphSampleMode = GetValue(o.GraphSampleMode, _graphSampleModeOption, c);

            o.WorkingDir = GetValue(o.WorkingDir, _workingDirOption, c);
            o.StatusFile = GetValue(o.StatusFile, _statusFilenameOption, c);

            return o;
        }

        private static T GetValue<T>(T defaultValue, Option<T>? option, BindingContext context)
        {
            if (option == null)
                return defaultValue;

            var valueGiven = context.ParseResult.FindResultFor(option) != null;

            if (!valueGiven)
                return defaultValue;

            var value = context.ParseResult.GetValueForOption(option);
            if (value == null)
                return defaultValue;

            return value;
        }
    }
}
