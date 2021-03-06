using Orleans.Concurrency;
using Orleans.Runtime;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-bucket in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Reentrant]
    public abstract class HashIndexPartitionedPerKeyBucket<K, V> : HashIndexSingleBucket<K, V>, IHashIndexPartitionedPerKeyBucketInterface<K, V> where V : class, IIndexableGrain
    {
    }
}
