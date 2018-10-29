using System;

namespace Orleans.Indexing
{
    public interface IIndexingOptions
    {
        int MaxHashBuckets { get; set; }
    }
    public class IndexingOptions : IIndexingOptions
    {
        public IndexingOptions()
        {
            this.MaxHashBuckets = -1;
        }

        public int MaxHashBuckets { get; set; }
    }
}
