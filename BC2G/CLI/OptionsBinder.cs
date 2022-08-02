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
        private readonly Option<GraphSampleMode>? _graphSampleModeOption;

        public OptionsBinder(

            Option<int>? fromInclusiveOption = null,
            Option<int>? toExclusiveOption = null,
            Option<int>? graphSampleCountOption = null,
            Option<GraphSampleMode>? graphSampleModeOption = null,
            Option<int>? granularityOption = null)
        {
            _fromInclusiveOption = fromInclusiveOption;
            _toExclusiveOption = toExclusiveOption;
            _granularityOption = granularityOption;
            _graphSampleCountOption = graphSampleCountOption;
            _graphSampleModeOption = graphSampleModeOption;
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

            if (_graphSampleModeOption != null)
                o.GraphSampleMode = c.ParseResult.GetValueForOption(_graphSampleModeOption);

            return o;
        }
    }
}
