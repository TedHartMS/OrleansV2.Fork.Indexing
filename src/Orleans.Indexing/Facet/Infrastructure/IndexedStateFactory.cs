using System;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Runtime;

namespace Orleans.Indexing.Facet
{
    public class IndexedStateFactory : IIndexedStateFactory
    {
        private readonly IServiceProvider activationServices;

        public IndexedStateFactory(IGrainActivationContext context, ITypeResolver typeResolver, IGrainFactory grainFactory)
        {
            this.activationServices = context.ActivationServices;
        }

        public INonFaultTolerantWorkflowIndexedState<TState> CreateNonFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config)
            where TState : class, new()
            => this.CreateIndexedState<NonFaultTolerantWorkflowIndexedState<TState>>(config);

        public IFaultTolerantWorkflowIndexedState<TState> CreateFaultTolerantWorkflowIndexedState<TState>(IIndexedStateConfiguration config)
            where TState : class, new()
            => this.CreateIndexedState<FaultTolerantWorkflowIndexedState<TState>>(config);

        private TWrappedIndexedStateImplementation CreateIndexedState<TWrappedIndexedStateImplementation>(IIndexedStateConfiguration config)
            => ActivatorUtilities.CreateInstance<TWrappedIndexedStateImplementation>(
                    this.activationServices, config);
    }
}
