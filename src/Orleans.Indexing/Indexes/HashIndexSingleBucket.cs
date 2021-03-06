using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Concurrency;
using Orleans.Runtime;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Orleans.Indexing
{
    /// <summary>
    /// A simple implementation of a single-grain in-memory hash-index
    /// </summary>
    /// <typeparam name="K">type of hash-index key</typeparam>
    /// <typeparam name="V">type of grain that is being indexed</typeparam>
    [Reentrant]
    public abstract class HashIndexSingleBucket<K, V> : Grain<HashIndexBucketState<K, V>>, IHashIndexSingleBucketInterface<K, V> where V : class, IIndexableGrain
    {
        // IndexManager (and therefore logger) cannot be set in ctor because Grain activation has not yet set base.Runtime.
        internal SiloIndexManager SiloIndexManager => IndexManager.GetSiloIndexManager(ref __siloIndexManager, base.ServiceProvider);
        private SiloIndexManager __siloIndexManager;

        private ILogger Logger => __logger ?? (__logger = this.SiloIndexManager.LoggerFactory.CreateLoggerWithFullCategoryName<HashIndexSingleBucket<K, V>>());
        private ILogger __logger;

        public override Task OnActivateAsync()
        {
            if (this.State.IndexMap == null) this.State.IndexMap = new Dictionary<K, HashIndexSingleBucketEntry<V>>();
            this.State.IndexStatus = IndexStatus.Available;
            //TODO: support for index construction should be added. Currently the Total indexes can only be defined in advance.
            //if (this.State.IndexStatus == IndexStatus.UnderConstruction)
            //{
            //    //Build the index!
            //}
            this.write_lock = new AsyncLock();
            this.writeRequestIdGen = 0;
            this.pendingWriteRequests = new HashSet<int>();
            return base.OnActivateAsync();
        }

#region Reentrant Index Update
#region Reentrant Index Update Variables

        /// <summary>
        /// This lock is used to queue all the writes to the storage and do them in a single batch, i.e., group commit
        /// 
        /// Works hand-in-hand with pendingWriteRequests and writeRequestIdGen.
        /// </summary>
        private AsyncLock write_lock;

        /// <summary>
        /// Creates a unique ID for each write request to the storage.
        /// 
        /// The values generated by this ID generator are used in pendingWriteRequests
        /// </summary>
        private int writeRequestIdGen;

        /// <summary>
        /// All write requests that are waiting behind write_lock are accumulated in this data structure, and all will be done at once.
        /// </summary>
        private HashSet<int> pendingWriteRequests;

#endregion Reentrant Index Update Variables

        public async Task<bool> DirectApplyIndexUpdateBatch(Immutable<IDictionary<IIndexableGrain, IList<IMemberUpdate>>> iUpdates, bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress = null)
        {
            this.Logger.Trace($"Started calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}, siloAddress = {siloAddress}, iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}");

            await Task.WhenAll(iUpdates.Value.Select(kv => DirectApplyIndexUpdatesNonPersistent(kv.Key, kv.Value, isUnique, idxMetaData, siloAddress)));
            await PersistIndex();

            this.Logger.Trace($"Finished calling DirectApplyIndexUpdateBatch with the following parameters: isUnique = {isUnique}, siloAddress = {siloAddress}, iUpdates = {MemberUpdate.UpdatesToString(iUpdates.Value)}");

            return true;
        }

        private async Task<IIndexInterface<K, V>> GetNextBucketAndPersist()
        {
            IIndexInterface<K, V> nextBucket = GetNextBucket();
            await PersistIndex();
            return nextBucket;
        }

        internal abstract IIndexInterface<K, V> GetNextBucket();

        /// <summary>
        /// This method applies a given update to the current index.
        /// </summary>
        /// <param name="updatedGrain">the grain that issued the update</param>
        /// <param name="iUpdate">contains the data for the update</param>
        /// <param name="isUnique">whether this is a unique index that we are updating</param>
        /// <param name="idxMetaData">the index metadata</param>
        /// <param name="siloAddress">the silo address</param>
        /// <returns>true, if the index update was successful, otherwise false</returns>
        public async Task<bool> DirectApplyIndexUpdate(IIndexableGrain updatedGrain, Immutable<IMemberUpdate> iUpdate, bool isUnique, IndexMetaData idxMetaData, SiloAddress siloAddress)
        {
            await DirectApplyIndexUpdateNonPersistent(updatedGrain, iUpdate.Value, isUnique, idxMetaData, siloAddress);
            await PersistIndex();
            return true;
        }

        private Task DirectApplyIndexUpdatesNonPersistent(IIndexableGrain g, IList<IMemberUpdate> updates, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
            => Task.WhenAll(updates.Select(updt => DirectApplyIndexUpdateNonPersistent(g, updt, isUniqueIndex, idxMetaData, siloAddress)));

        private async Task DirectApplyIndexUpdateNonPersistent(IIndexableGrain g, IMemberUpdate updt, bool isUniqueIndex, IndexMetaData idxMetaData, SiloAddress siloAddress)
        {
            // The target grain that is updated
            V updatedGrain = g.AsReference<V>(this.SiloIndexManager);

            // Updates the index bucket synchronously (note that no other thread can run concurrently before we reach an await operation,
            // when execution is yielded back to the Orleans scheduler, so no concurrency control mechanism (e.g., locking) is required).
            // 'fixIndexUnavailableOnDelete' indicates whether the index was still unavailable when we received a delete operation.
            if (!HashIndexBucketUtils.UpdateBucketState(updatedGrain, updt, this.State, isUniqueIndex, idxMetaData, out K befImg, out HashIndexSingleBucketEntry<V> befEntry, out bool fixIndexUnavailableOnDelete))
            {
                await (await GetNextBucketAndPersist()).DirectApplyIndexUpdate(g, updt.AsImmutable(), isUniqueIndex, idxMetaData, siloAddress);
            }

            // TODO if the index was still unavailable when we received a delete operation
            //if (fixIndexUnavailableOnDelete)
            //{
            //    //create tombstone
            //}
        }

        /// <summary>
        /// Persists the state of the index
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task PersistIndex()
        {
            //create a write-request ID, which is used for group commit
            int writeRequestId = ++this.writeRequestIdGen;

            //add the write-request ID to the pending write requests
            this.pendingWriteRequests.Add(writeRequestId);

            //wait before any previous write is done
            using (await this.write_lock.LockAsync())
            {
                // If the write request is not there, it was handled by another worker before we obtained the lock.
                if (this.pendingWriteRequests.Contains(writeRequestId))
                {
                    //clear all pending write requests, as this attempt will do them all.
                    this.pendingWriteRequests.Clear();

                    // Write the index state back to the storage. TODO: What is the best way to handle an index write error?
                    int numRetries = 0;
                    while (true)
                    {
                        try
                        {
                            await base.WriteStateAsync();
                            return;
                        }
                        catch when (numRetries < 3)
                        {
                            ++numRetries;
                            await Task.Delay(100);
                        }
                    }
                }
            }
        }
#endregion Reentrant Index Update

        private Exception LogException(string message, IndexingErrorCode errorCode)
        {
            var e = new Exception(message);
            this.Logger.Error(errorCode, message, e);
            return e;
        }

        public async Task Lookup(IOrleansQueryResultStream<V> result, K key)
        {
            this.Logger.Trace($"Streamed index lookup called for key = {key}");

            if (!(this.State.IndexStatus == IndexStatus.Available))
            {
                throw LogException("Index is not still available", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainBucket1);
            }
            if (this.State.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry))
            {
                if (!entry.IsTentative)
                {
                    await result.OnNextBatchAsync(entry.Values);
                }
                await result.OnCompletedAsync();
            }
            else if (this.State.NextBucket != null)
            {
                await GetNextBucket().Lookup(result, key);
            }
            else
            {
                await result.OnCompletedAsync();
            }
        }

        public async Task<V> LookupUnique(K key)
        {
            if (!(this.State.IndexStatus == IndexStatus.Available))
            {
                throw LogException("Index is not still available", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainBucket2);
            }
            if (this.State.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry))
            {
                return (entry.Values.Count == 1 && !entry.IsTentative)
                    ? entry.Values.GetEnumerator().Current
                    : throw LogException($"There are {entry.Values.Count} values for the unique lookup key \"{key}\" on index" +
                                         $" \"{IndexUtils.GetIndexNameFromIndexGrain(this)}\", and the entry is{(entry.IsTentative ? "" : " not")} tentative.",
                                        IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainBucket3);
            }
            return (this.State.NextBucket != null)
                ? await ((IHashIndexInterface<K, V>)GetNextBucket()).LookupUnique(key)
                : throw LogException($"The lookup key \"{key}\" does not exist on index \"{IndexUtils.GetIndexNameFromIndexGrain(this)}\".",
                                    IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainBucket4);
        }

        public Task Dispose()
        {
            this.State.IndexStatus = IndexStatus.Disposed;
            this.State.IndexMap.Clear();
            base.DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public Task<bool> IsAvailable() => Task.FromResult(this.State.IndexStatus == IndexStatus.Available);

        Task IIndexInterface.Lookup(IOrleansQueryResultStream<IIndexableGrain> result, object key) => Lookup(result.Cast<V>(), (K)key);

        public async Task<IOrleansQueryResult<V>> Lookup(K key)
        {
            this.Logger.Trace($"Eager index lookup called for key = {key}");

            if (!(this.State.IndexStatus == IndexStatus.Available))
            {
                throw LogException("Index is not still available.", IndexingErrorCode.IndexingIndexIsNotReadyYet_GrainBucket5);
            }
            if (this.State.IndexMap.TryGetValue(key, out HashIndexSingleBucketEntry<V> entry) && !entry.IsTentative)
            {
                return new OrleansQueryResult<V>(entry.Values);
            }
            return (this.State.NextBucket != null)
                ? await GetNextBucket().Lookup(key)
                : new OrleansQueryResult<V>(Enumerable.Empty<V>());
        }

        async Task<IOrleansQueryResult<IIndexableGrain>> IIndexInterface.Lookup(object key) => await Lookup((K)key);
    }
}
