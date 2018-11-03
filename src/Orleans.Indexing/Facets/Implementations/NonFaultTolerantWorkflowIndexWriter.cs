using System;
using System.Threading.Tasks;
using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class NonFaultTolerantWorkflowIndexWriter<TGrainState> : WorkflowIndexWriterBase<TGrainState>,
                                                                    INonFaultTolerantWorkflowIndexWriter<TGrainState> where TGrainState : new()
    {
        public NonFaultTolerantWorkflowIndexWriter(
                IGrainActivationContext context,
                IIndexWriterConfiguration config
            ) : base(context, config)
        {
        }

        public Task Write(Grain<TGrainState> grain, Func<Task> stateUpdateAction) => throw new NotImplementedException("TODO");
    }
}
