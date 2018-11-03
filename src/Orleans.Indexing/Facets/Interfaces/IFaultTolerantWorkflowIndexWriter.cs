namespace Orleans.Indexing.Facets
{
    public interface IFaultTolerantWorkflowIndexWriter<TGrainState> : IIndexWriter<TGrainState> where TGrainState : new()
    {
    }

}
