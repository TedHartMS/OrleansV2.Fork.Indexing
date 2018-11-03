using System;

namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// Base class for the IIndexWriter facet that is implemented by non-fault-tolerant workflow-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class NonFaultTolerantWorkflowIndexWriterAttribute : Attribute, IFacetMetadata, IIndexWriterConfiguration
    {
        public string StorageName { get; }

        public NonFaultTolerantWorkflowIndexWriterAttribute(string storageName = null)
        {
            this.StorageName = storageName;
        }
    }
}
