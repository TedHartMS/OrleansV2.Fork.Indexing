using Orleans.Providers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey

    // NFT only; FT cannot be configured to be Eager.

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_EG_PK : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = true, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_State_UIUSNINS_DSMI_EG_PK : NFT_Props_UIUSNINS_DSMI_EG_PK, ITestMultiIndexState
    {
        public string UnIndexedString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_DSMI_EG_PK : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_EG_PK>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_EG_PK : TestMultiIndexGrainNonFaultTolerant<NFT_State_UIUSNINS_DSMI_EG_PK, NFT_Props_UIUSNINS_DSMI_EG_PK>,
                                                 INFT_Grain_UIUSNINS_DSMI_EG_PK
    {
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // NFT only; FT cannot be configured to be Eager.

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_EG_PS : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = true, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_State_UIUSNINS_DSMI_EG_PS : NFT_Props_UIUSNINS_DSMI_EG_PS, ITestMultiIndexState
    {
        public string UnIndexedString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_DSMI_EG_PS : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_EG_PS>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_EG_PS : TestMultiIndexGrainNonFaultTolerant<NFT_State_UIUSNINS_DSMI_EG_PS, NFT_Props_UIUSNINS_DSMI_EG_PS>,
                                                 INFT_Grain_UIUSNINS_DSMI_EG_PS
    {
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket

    // NFT only; FT cannot be configured to be Eager.

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_EG_SB : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = true, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = true, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_State_UIUSNINS_DSMI_EG_SB : NFT_Props_UIUSNINS_DSMI_EG_SB, ITestMultiIndexState
    {
        public string UnIndexedString { get; set; }
    }

    public interface INFT_Grain_UIUSNINS_DSMI_EG_SB : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_EG_SB>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_EG_SB : TestMultiIndexGrainNonFaultTolerant<NFT_State_UIUSNINS_DSMI_EG_SB, NFT_Props_UIUSNINS_DSMI_EG_SB>,
                                                 INFT_Grain_UIUSNINS_DSMI_EG_SB
    {
    }
    #endregion // SingleBucket

    public abstract class MultiIndex_DSMI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_DSMI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_EG_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_EG_PK, NFT_Props_UIUSNINS_DSMI_EG_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_EG_PS()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_EG_PS, NFT_Props_UIUSNINS_DSMI_EG_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_EG_SB()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_EG_SB, NFT_Props_UIUSNINS_DSMI_EG_SB>();
        }
    }
}
