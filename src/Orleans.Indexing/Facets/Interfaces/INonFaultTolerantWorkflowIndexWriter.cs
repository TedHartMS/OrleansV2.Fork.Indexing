namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// The interface definition for a class that implements the indexing facet of a grain using a workflow
    /// implementation that is not fault-tolerant.
    /// </summary>
    /// <typeparam name="TGrainState">The state implementation class of a <see cref="Grain{TGrainState}"/>.</typeparam>
    public interface INonFaultTolerantWorkflowIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }
}
