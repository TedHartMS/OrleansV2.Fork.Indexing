using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facets
{
    public class IndexWriterFactory : IIndexWriterFactory
    {
        private readonly IServiceProvider activationServices;

        public IndexWriterFactory(IGrainActivationContext context, ITypeResolver typeResolver, IGrainFactory grainFactory)
        {
            this.activationServices = context.ActivationServices;
        }

        public INonFaultTolerantWorkflowIndexWriter<TState> CreateNonFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config)
            where TState : class, new()
            => this.CreateIndexWriter<NonFaultTolerantWorkflowIndexWriter<TState>>(config);

        public IFaultTolerantWorkflowIndexWriter<TState> CreateFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config)
            where TState : class, new()
            => this.CreateIndexWriter<FaultTolerantWorkflowIndexWriter<TState>>(config);

        private TIndexWriterImplementationWithState CreateIndexWriter<TIndexWriterImplementationWithState>(IIndexWriterConfiguration config)
            => ActivatorUtilities.CreateInstance<TIndexWriterImplementationWithState>(
                    this.activationServices, config);
    }
}
