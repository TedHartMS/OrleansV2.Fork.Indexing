using System.Linq;

namespace Orleans.Indexing
{
    internal static class HashIndexBucketUtils
    {
        /// <summary>
        /// This method contains the common functionality for updating the hash-index bucket.
        /// </summary>
        /// <typeparam name="K">key type</typeparam>
        /// <typeparam name="V">value type</typeparam>
        /// <param name="updatedGrain">the updated grain that is being indexed</param>
        /// <param name="iUpdate">the update information</param>
        /// <param name="state">the index bucket to be updated</param>
        /// <param name="isUniqueIndex">a flag to indicate whether the hash-index has a uniqueness constraint</param>
        /// <param name="idxMetaData">the index metadata</param>
        internal static bool UpdateBucket<K, V>(V updatedGrain, IMemberUpdate iUpdate, HashIndexBucketState<K, V> state, bool isUniqueIndex,
                                                IndexMetaData idxMetaData) where V : IIndexableGrain
            => UpdateBucket(updatedGrain, iUpdate, state, isUniqueIndex, idxMetaData, out K befImg, out HashIndexSingleBucketEntry<V> _, out bool _);

        /// <summary>
        /// This method contains the common functionality for updating the hash-index bucket.
        /// </summary>
        /// <typeparam name="K">key type</typeparam>
        /// <typeparam name="V">value type</typeparam>
        /// <param name="updatedGrain">the updated grain that is being indexed</param>
        /// <param name="update">the update information</param>
        /// <param name="state">the index bucket to be updated</param>
        /// <param name="isUniqueIndex">a flag to indicate whether the hash-index has a uniqueness constraint</param>
        /// <param name="idxMetaData">the index metadata</param>
        /// <param name="befImg">output parameter: the before-image</param>
        /// <param name="befEntry">output parameter: the index entry containing the before-image</param>
        /// <param name="fixIndexUnavailableOnDelete">output parameter: this variable determines whether
        ///             the index was still unavailable when we received a delete operation</param>
        internal static bool UpdateBucket<K, V>(V updatedGrain, IMemberUpdate update, HashIndexBucketState<K, V> state, bool isUniqueIndex,
                                                IndexMetaData idxMetaData, out K befImg, out HashIndexSingleBucketEntry<V> befEntry,
                                                out bool fixIndexUnavailableOnDelete) where V : IIndexableGrain
        {
            fixIndexUnavailableOnDelete = false;
            befImg = default(K);
            befEntry = null;

            bool isTentativeUpdate = isUniqueIndex && (update is MemberUpdateTentative);
            IndexOperationType opType = update.OperationType;
            HashIndexSingleBucketEntry<V> aftEntry;
            if (opType == IndexOperationType.Update)
            {
                befImg = (K)update.GetBeforeImage();
                K aftImg = (K)update.GetAfterImage();
                if (state.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                {   //Delete and Insert
                    if (state.IndexMap.TryGetValue(aftImg, out aftEntry))
                    {
                        if (aftEntry.Values.Contains(updatedGrain))
                        {
                            if (isTentativeUpdate)
                            {
                                aftEntry.SetTentativeInsert();
                            }
                            else
                            {
                                aftEntry.ClearTentativeFlag();
                                befEntry.Remove(updatedGrain, isTentativeUpdate, isUniqueIndex);
                            }
                        }
                        else
                        {
                            if (isUniqueIndex && aftEntry.Values.Count > 0)
                            {
                                throw new UniquenessConstraintViolatedException(
                                        $"The uniqueness property of index {idxMetaData.IndexName} is would be violated for an update operation" +
                                        $" for before-image = {befImg}, after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}");
                            }
                            // TODO: Possible race condition as in Insert below (recovery would have to restore to befEntry).
                            befEntry.Remove(updatedGrain, isTentativeUpdate, isUniqueIndex);
                            aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        }
                    }
                    else
                    {
                        aftEntry = new HashIndexSingleBucketEntry<V>();
                        befEntry.Remove(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        state.IndexMap.Add(aftImg, aftEntry);
                    }
                }
                else
                {
                    if (idxMetaData.IsChainedBuckets)
                    {
                        return false; //not found in this bucket
                    }
                    //Insert
                    if (state.IndexMap.TryGetValue(aftImg, out aftEntry))
                    {
                        if (!aftEntry.Values.Contains(updatedGrain))
                        {
                            if (isUniqueIndex && aftEntry.Values.Count > 0)
                            {
                                throw new UniquenessConstraintViolatedException(
                                        $"The uniqueness property of index {idxMetaData.IndexName} would be violated for an update operation" +
                                        $" for (not found before-image = {befImg}), after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}");
                            }
                            // TODO Possible race condition as in Insert below
                            aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        }
                        else if (isTentativeUpdate)
                        {
                            aftEntry.SetTentativeInsert();
                        }
                        else
                        {
                            aftEntry.ClearTentativeFlag();
                        }
                    }
                    else
                    {
                        if (idxMetaData.IsCreatingANewBucketNecessary(state.IndexMap.Count()))
                        {
                            return false;
                        }
                        aftEntry = new HashIndexSingleBucketEntry<V>();
                        aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        state.IndexMap.Add(aftImg, aftEntry);
                    }
                }
            }
            else if (opType == IndexOperationType.Insert)
            { // Insert
                K aftImg = (K)update.GetAfterImage();
                if (state.IndexMap.TryGetValue(aftImg, out aftEntry))
                {
                    if (!aftEntry.Values.Contains(updatedGrain))
                    {
                        if (isUniqueIndex && aftEntry.Values.Count > 0)
                        {
                            throw new UniquenessConstraintViolatedException(
                                    $"The uniqueness property of index {idxMetaData.IndexName} would be violated for an insert operation" +
                                    $" for after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}");
                        }
                        aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);
                        if (isUniqueIndex && aftEntry.Values.Count > 1)
                        {
                            // TODO: diagnose and better fix for a possible race condition; Test_MultiInterface_All occasionally returns
                            // a count of 2 when edited to allow duplicate values for "title_2" (Title has a unique index) when this
                            // check does not fire (this one didn't fire either, but that could be part of the race).
                            aftEntry.Remove(updatedGrain, isTentativeUpdate, isUniqueIndex);
                            throw new UniquenessConstraintViolatedException(
                                    $"The uniqueness property of index {idxMetaData.IndexName} has been violated after an insert operation" +
                                    $" for after-image = {aftImg} and grain = {updatedGrain.GetPrimaryKey()}");
                        }
                    }
                    else if (isTentativeUpdate)
                    {
                        aftEntry.SetTentativeInsert();
                    }
                    else
                    {
                        aftEntry.ClearTentativeFlag();
                    }
                }
                else
                {
                    if (idxMetaData.IsCreatingANewBucketNecessary(state.IndexMap.Count()))
                    {
                        return false;  //the bucket is full
                    }
                    aftEntry = new HashIndexSingleBucketEntry<V>();
                    aftEntry.Add(updatedGrain, isTentativeUpdate, isUniqueIndex);

                    // TODO One Add() could be lost if two threads simultaneously found no aftEntry
                    state.IndexMap.Add(aftImg, aftEntry);
                }
            }
            else if (opType == IndexOperationType.Delete)
            { // Delete
                befImg = (K)update.GetBeforeImage();

                if (state.IndexMap.TryGetValue(befImg, out befEntry) && befEntry.Values.Contains(updatedGrain))
                {
                    befEntry.Remove(updatedGrain, isTentativeUpdate, isUniqueIndex);
                    if (state.IndexStatus != IndexStatus.Available)
                    {
                        fixIndexUnavailableOnDelete = true;
                    }
                }
                else if (idxMetaData.IsChainedBuckets)
                {
                    return false; //not found in this bucket
                }
            }
            return true;
        }
    }
}
