using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class IndexWriterFactory : IIndexWriterFactory
    {
        private readonly IGrainActivationContext context;   // TODO is this needed?

        public IndexWriterFactory(IGrainActivationContext context, ITypeResolver typeResolver, IGrainFactory grainFactory)
        {
            this.context = context;
        }

        public INonFaultTolerantWorkflowIndexWriter<TState> CreateNonFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config)
            where TState : class, new()
            => this.CreateIndexWriter<NonFaultTolerantWorkflowIndexWriter<TState>>(config);

        public IFaultTolerantWorkflowIndexWriter<TState> CreateFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config)
            where TState : class, new()
            => this.CreateIndexWriter<FaultTolerantWorkflowIndexWriter<TState>>(config);

        private TIndexWriterImplementationWithState CreateIndexWriter<TIndexWriterImplementationWithState>(IIndexWriterConfiguration config)
            => ActivatorUtilities.CreateInstance<TIndexWriterImplementationWithState>(
                    this.context.ActivationServices, config, this.context);
    }
}
