using System;

namespace Orleans.Indexing.Facet
{
    /// <summary>
    /// Marker interface for fault-tolerant index writer.
    /// </summary>
    public interface IFaultTolerantWorkflowIndexWriterAttribute
    {
    }

    /// <summary>
    /// Base class for the IIndexWriter facet that is implemented by fault-tolerant workflow-based indexing.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FaultTolerantWorkflowIndexWriterAttribute : Attribute, IFacetMetadata, IFaultTolerantWorkflowIndexWriterAttribute, IIndexWriterConfiguration
    {
        public string StorageName { get; }

        public FaultTolerantWorkflowIndexWriterAttribute(string storageName = null)
        {
            this.StorageName = storageName;
        }
    }
}
