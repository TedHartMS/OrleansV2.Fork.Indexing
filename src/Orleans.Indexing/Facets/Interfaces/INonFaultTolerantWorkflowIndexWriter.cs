namespace Orleans.Indexing.Facets
{
    public interface INonFaultTolerantWorkflowIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }
}
