using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class FaultTolerantWorkflowIndexWriter<TGrainState> : WorkflowIndexWriterBase<TGrainState>,
                                                                 IFaultTolerantWorkflowIndexWriter<TGrainState> where TGrainState : new()
    {
        public FaultTolerantWorkflowIndexWriter(
            IGrainActivationContext context,
            IIndexWriterConfiguration config
            ) : base(context, config)
        {
        }

        public Task Write(Grain<TGrainState> grain, Func<Task> stateUpdateAction) => throw new NotImplementedException("TODO");
    }
}
