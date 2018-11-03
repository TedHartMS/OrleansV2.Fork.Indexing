namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// Per-instance configuration information. The <see cref="FaultTolerantWorkflowIndexWriterAttribute"/>
    /// and <see cref="NonFaultTolerantWorkflowIndexWriterAttribute"/> classes implement this, which is how
    /// the attribute parameters are communicated to the <see cref="IIndexWriter{TGrainState}"/> implementation.
    /// </summary>
    public interface IIndexWriterConfiguration
    {
        string StorageName { get; }
    }
}
