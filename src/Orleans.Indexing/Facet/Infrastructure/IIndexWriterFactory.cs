namespace Orleans.Indexing.Facet
{
    public interface IIndexWriterFactory
    {
        INonFaultTolerantWorkflowIndexWriter<TState> CreateNonFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config) where TState : class, new();
        IFaultTolerantWorkflowIndexWriter<TState> CreateFaultTolerantWorkflowIndexWriter<TState>(IIndexWriterConfiguration config) where TState : class, new();
    }
}
