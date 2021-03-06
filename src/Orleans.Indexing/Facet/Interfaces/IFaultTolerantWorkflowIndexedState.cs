namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// The interface definition for a class that implements the indexing facet of a grain using a workflow
    /// implementation that is fault-tolerant.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class of a <see cref="Grain{WrappedTGrainState}"/>.
    ///     Note that for fault-tolerant indexing, the grain's state must be wrapped by <see cref="FaultTolerantIndexedGrainStateWrapper{TGrainState}"/>
    ///     to persist the necessary indexing structures.
    /// </typeparam>
    public interface IFaultTolerantWorkflowIndexedState<TGrainState> : IIndexedState<TGrainState> where TGrainState : new()
    {
    }
}
