namespace BC2G.CommandLineInterface
{
    internal class OptionsBinder : BinderBase<Options>
    {
        private readonly Options _options;
        private readonly Option<int?>? _fromInclusiveOption;
        private readonly Option<int?>? _toExclusiveOption;
        private readonly Option<int>? _granularityOption;
        private readonly Option<bool>? _skipGraphLoadOption;
        private readonly Option<Uri>? _bitcoinClientUri;
        private readonly Option<int>? _graphSampleCountOption;
        private readonly Option<int>? _graphSampleHopsOption;
        private readonly Option<int>? _graphSampleMinNodeCount;
        private readonly Option<int>? _graphSampleMaxNodeCount;
        private readonly Option<int>? _graphSampleMinEdgeCount;
        private readonly Option<int>? _graphSampleMaxEdgeCount;
        private readonly Option<GraphSampleMode>? _graphSampleModeOption;
        private readonly Option<double>? _graphSampleRootNodeSelectProb;
        private readonly Option<string>? _workingDirOption;
        private readonly Option<string>? _statusFilenameOption;

        public OptionsBinder(
            Options options,
            Option<int?>? fromInclusiveOption = null,
            Option<int?>? toExclusiveOption = null,
            Option<int>? granularityOption = null,
            Option<bool>? skipGraphLoadOption = null,
            Option<Uri>? bitcoinClientUri = null,
            Option<int>? graphSampleCountOption = null,
            Option<int>? graphSampleHopOption = null,
            Option<int>? graphSampleMinNodeCount = null,
            Option<int>? graphSampleMaxNodeCount = null,
            Option<int>? graphSampleMinEdgeCount = null,
            Option<int>? graphSampleMaxEdgeCount = null,
            Option<GraphSampleMode>? graphSampleModeOption = null,
            Option<double>? graphSampleRootNodeSelectProb = null,
            Option<string>? workingDirOption = null,
            Option<string>? statusFilenameOption = null)
        {
            _options = options;
            _fromInclusiveOption = fromInclusiveOption;
            _toExclusiveOption = toExclusiveOption;
            _granularityOption = granularityOption;
            _skipGraphLoadOption = skipGraphLoadOption;
            _bitcoinClientUri = bitcoinClientUri;
            _graphSampleCountOption = graphSampleCountOption;
            _graphSampleHopsOption = graphSampleHopOption;
            _graphSampleMinNodeCount = graphSampleMinNodeCount;
            _graphSampleMaxNodeCount = graphSampleMaxNodeCount;
            _graphSampleMinEdgeCount = graphSampleMinEdgeCount;
            _graphSampleMaxEdgeCount = graphSampleMaxEdgeCount;
            _graphSampleModeOption = graphSampleModeOption;
            _graphSampleRootNodeSelectProb = graphSampleRootNodeSelectProb;
            _workingDirOption = workingDirOption;
            _statusFilenameOption = statusFilenameOption;
        }

        protected override Options GetBoundValue(BindingContext c)
        {
            var o = _options;
            o.WorkingDir = GetValue(o.WorkingDir, _workingDirOption, c);
            o.StatusFile = GetValue(o.StatusFile, _statusFilenameOption, c);

            o.Bitcoin.FromInclusive = GetValue(o.Bitcoin.FromInclusive, _fromInclusiveOption, c);
            o.Bitcoin.ToExclusive = GetValue(o.Bitcoin.ToExclusive, _toExclusiveOption, c);
            o.Bitcoin.Granularity = GetValue(o.Bitcoin.Granularity, _granularityOption, c);
            o.Bitcoin.SkipGraphLoad = GetValue(o.Bitcoin.SkipGraphLoad, _skipGraphLoadOption, c);
            o.Bitcoin.ClientUri = GetValue(o.Bitcoin.ClientUri, _bitcoinClientUri, c);

            o.GraphSample.Count = GetValue(o.GraphSample.Count, _graphSampleCountOption, c);
            o.GraphSample.Hops = GetValue(o.GraphSample.Hops, _graphSampleHopsOption, c);
            o.GraphSample.MinNodeCount = GetValue(o.GraphSample.MinNodeCount, _graphSampleMinNodeCount, c);
            o.GraphSample.MaxNodeCount = GetValue(o.GraphSample.MaxNodeCount, _graphSampleMaxNodeCount, c);
            o.GraphSample.MinEdgeCount = GetValue(o.GraphSample.MinEdgeCount, _graphSampleMinEdgeCount, c);
            o.GraphSample.MaxEdgeCount = GetValue(o.GraphSample.MaxEdgeCount, _graphSampleMaxEdgeCount, c);
            o.GraphSample.Mode = GetValue(o.GraphSample.Mode, _graphSampleModeOption, c);
            o.GraphSample.RootNodeSelectProb = GetValue(o.GraphSample.RootNodeSelectProb, _graphSampleRootNodeSelectProb, c);
            
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
