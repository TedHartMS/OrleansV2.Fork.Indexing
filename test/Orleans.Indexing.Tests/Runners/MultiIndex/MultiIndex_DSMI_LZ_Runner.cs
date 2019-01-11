using Orleans.Providers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests
{
    #region PartitionedPerKey
    [Serializable]
    public class FT_Props_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    // TODO: Indexes are based on InterfaceType but not ClassType, so currently, unique index tests run in parallel must have 
    // distinct interfaces, which percolates to state and properties as well.
    public interface IFT_Grain_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_PK>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_PK : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_PK>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_PK : TestMultiIndexGrain<TestMultiIndexState, FT_Props_UIUSNINS_DSMI_LZ_PK>, IFT_Grain_UIUSNINS_DSMI_LZ_PK
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_PK : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState, NFT_Props_UIUSNINS_DSMI_LZ_PK>, INFT_Grain_UIUSNINS_DSMI_LZ_PK
    {
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo
    [Serializable]
    public class FT_Props_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]    // PerSilo cannot be Unique
        public int UniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false)]  // PerSilo cannot be Unique
        public string UniqueString { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_UIUSNINS_DSMI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    // TODO: Indexes are based on InterfaceType but not ClassType, so currently, unique index tests run in parallel must have 
    // distinct interfaces, which percolates to state and properties as well.
    public interface IFT_Grain_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_PS>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_PS : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_PS>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_PS : TestMultiIndexGrain<TestMultiIndexState, FT_Props_UIUSNINS_DSMI_LZ_PS>, IFT_Grain_UIUSNINS_DSMI_LZ_PS
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_PS : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState, NFT_Props_UIUSNINS_DSMI_LZ_PS>, INFT_Grain_UIUSNINS_DSMI_LZ_PS
    {
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket
    [Serializable]
    public class FT_Props_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "0")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "-1")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    [Serializable]
    public class NFT_Props_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexProperties
    {
        [StorageManagedIndex(IsEager = false, IsUnique = true, NullValue = "-1")]
        public int UniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = true)]
        public string UniqueString { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false, NullValue = "0")]
        public int NonUniqueInt { get; set; }

        [StorageManagedIndex(IsEager = false, IsUnique = false)]
        public string NonUniqueString { get; set; }
    }

    // TODO: Indexes are based on InterfaceType but not ClassType, so currently, unique index tests run in parallel must have 
    // distinct interfaces, which percolates to state and properties as well.
    public interface IFT_Grain_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<FT_Props_UIUSNINS_DSMI_LZ_SB>
    {
    }

    public interface INFT_Grain_UIUSNINS_DSMI_LZ_SB : ITestMultiIndexGrain, IIndexableGrain<NFT_Props_UIUSNINS_DSMI_LZ_SB>
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class FT_Grain_UIUSNINS_DSMI_LZ_SB : TestMultiIndexGrain<TestMultiIndexState, FT_Props_UIUSNINS_DSMI_LZ_SB>, IFT_Grain_UIUSNINS_DSMI_LZ_SB
    {
    }

    [StorageProvider(ProviderName = IndexingTestConstants.CosmosDBGrainStorage)]
    public class NFT_Grain_UIUSNINS_DSMI_LZ_SB : TestMultiIndexGrainNonFaultTolerant<TestMultiIndexState, NFT_Props_UIUSNINS_DSMI_LZ_SB>, INFT_Grain_UIUSNINS_DSMI_LZ_SB
    {
    }
    #endregion // SingleBucket

    public abstract class MultiIndex_DSMI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiIndex_DSMI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_PK, FT_Props_UIUSNINS_DSMI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_PK()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_PK, NFT_Props_UIUSNINS_DSMI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_PS()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_PS, FT_Props_UIUSNINS_DSMI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_PS()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_PS, NFT_Props_UIUSNINS_DSMI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_UIUSNINS_DSMI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<IFT_Grain_UIUSNINS_DSMI_LZ_SB, FT_Props_UIUSNINS_DSMI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_UIUSNINS_DSMI_LZ_SB()
        {
            await base.TestIndexesWithDeactivations<INFT_Grain_UIUSNINS_DSMI_LZ_SB, NFT_Props_UIUSNINS_DSMI_LZ_SB>();
        }
    }
}
