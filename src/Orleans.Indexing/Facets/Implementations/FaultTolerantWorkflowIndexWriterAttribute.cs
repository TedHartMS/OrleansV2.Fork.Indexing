using System;

namespace Orleans.Indexing.Facets
{
    /// <summary>
    /// Base class for the IIndexWriter facet that is implemented by fault-tolerant workflow-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FaultTolerantWorkflowIndexWriterAttribute : Attribute, IFacetMetadata, IIndexWriterConfiguration
    {
        public string StorageName { get; }

        public FaultTolerantWorkflowIndexWriterAttribute(string storageName = null)
        {
            this.StorageName = storageName;
        }
    }
}
