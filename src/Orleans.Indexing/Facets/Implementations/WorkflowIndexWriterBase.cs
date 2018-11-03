using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class WorkflowIndexWriterBase<TGrainState> where TGrainState : new()
    {
        private readonly IGrainActivationContext context;
        private readonly IIndexWriterConfiguration config;

        public WorkflowIndexWriterBase(IGrainActivationContext context, IIndexWriterConfiguration config)
        {
            this.context = context;
            this.config = config;
        }
    }
}
