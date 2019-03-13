using Orleans.Indexing.Facet;
using Orleans.Providers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey

    // NFT only; FT cannot be configured to be Eager.

    public class NFT_Props_UIUSNINS_XI_EG_PK : ITestMultiIndexProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<int, INFT_Grain_UIUSNINS_XI_EG_PK>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_UIUSNINS_XI_EG_PK>), IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_UIUSNINS_XI_EG_PK>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, INFT_Grain_UIUSNINS_XI_EG_PK>), IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_XI_EG_PK : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_XI_EG_PK>
    {
    }

    public class NFT_Grain_UIUSNINS_XI_EG_PK : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_XI_EG_PK
    {
        public NFT_Grain_UIUSNINS_XI_EG_PK(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // None; Total indexes cannot be specified as partitioned per silo.

    #endregion // PartitionedPerSilo

    #region SingleBucket

    // NFT only; FT cannot be configured to be Eager.

    public class NFT_Props_UIUSNINS_XI_EG_SB : ITestMultiIndexProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<int, INFT_Grain_UIUSNINS_XI_EG_SB>), IsEager = true, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_UIUSNINS_XI_EG_SB>), IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_UIUSNINS_XI_EG_SB>), IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<string, INFT_Grain_UIUSNINS_XI_EG_SB>), IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_XI_EG_SB : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_XI_EG_SB>
    {
    }

    public class NFT_Grain_UIUSNINS_XI_EG_SB : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState>, INFT_Grain_UIUSNINS_XI_EG_SB
    {
        public NFT_Grain_UIUSNINS_XI_EG_SB(
            [NonFaultTolerantWorkflowIndexedState(IndexingConstants.IndexedGrainStateName, IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
            IIndexedState<TestMultiIndexState> indexedState)
            : base(indexedState) { }
    }
    #endregion // SingleBucket

    public abstract class MultiIndex_XI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_XI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }


        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_XI_EG_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG_PK, NFT_Props_UIUSNINS_XI_EG_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_XI_EG_SB()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG_SB, NFT_Props_UIUSNINS_XI_EG_SB>();
        }

        internal static Func<IndexingTestRunnerBase, int, Task>[] GetAllTestTasks()
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG_PK, NFT_Props_UIUSNINS_XI_EG_PK>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_XI_EG_SB, NFT_Props_UIUSNINS_XI_EG_SB>(intAdjust)
            };
        }
    }
}
