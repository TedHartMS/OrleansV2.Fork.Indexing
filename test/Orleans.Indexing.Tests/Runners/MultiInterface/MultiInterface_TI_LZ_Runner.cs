using Orleans.Providers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facets;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    [Serializable]
    public class FT_Props_Person_TI_LZ_PK : IPersonProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Location { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, IFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class NFT_Props_Person_TI_LZ_PK : IPersonProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Location { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<int, INFT_Grain_Person_TI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class FT_Props_Job_TI_LZ_PK : IJobProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, IFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class NFT_Props_Job_TI_LZ_PK : IJobProperties
    {
        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(TotalHashIndexPartitionedPerKey<string, INFT_Grain_Job_TI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public interface IFT_Grain_Person_TI_LZ_PK : IIndexableGrain<FT_Props_Person_TI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_TI_LZ_PK : IIndexableGrain<NFT_Props_Person_TI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_TI_LZ_PK : IIndexableGrain<FT_Props_Job_TI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_LZ_PK : IIndexableGrain<NFT_Props_Job_TI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class FT_Grain_Employee_TI_LZ_PK : TestEmployeeGrain<EmployeeGrainState, FaultTolerantIndexableGrainStateWrapper<EmployeeGrainState>>,
                                              IFT_Grain_Person_TI_LZ_PK, IFT_Grain_Job_TI_LZ_PK
    {
        public FT_Grain_Employee_TI_LZ_PK(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class NFT_Grain_Employee_TI_LZ_PK : TestEmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               INFT_Grain_Person_TI_LZ_PK, INFT_Grain_Job_TI_LZ_PK
    {
        public NFT_Grain_Employee_TI_LZ_PK(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    // None; Total indexes cannot be specified as partitioned per silo.

    #endregion // PartitionedPerSilo

    #region SingleBucket

    // NFT only; FT cannot be configured to be Eager.

    [Serializable]
    public class FT_Props_Person_TI_LZ_SB : IPersonProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Location { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, IFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class NFT_Props_Person_TI_LZ_SB : IPersonProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Location { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<int, INFT_Grain_Person_TI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class FT_Props_Job_TI_LZ_SB : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, IFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class NFT_Props_Job_TI_LZ_SB : IJobProperties
    {
        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ITotalHashIndexSingleBucket<string, INFT_Grain_Job_TI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    public interface IFT_Grain_Person_TI_LZ_SB : IIndexableGrain<FT_Props_Person_TI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_TI_LZ_SB : IIndexableGrain<NFT_Props_Person_TI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_TI_LZ_SB : IIndexableGrain<FT_Props_Job_TI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_TI_LZ_SB : IIndexableGrain<NFT_Props_Job_TI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class FT_Grain_Employee_TI_LZ_SB : TestEmployeeGrain<EmployeeGrainState, FaultTolerantIndexableGrainStateWrapper<EmployeeGrainState>>,
                                              IFT_Grain_Person_TI_LZ_SB, IFT_Grain_Job_TI_LZ_SB
    {
        public FT_Grain_Employee_TI_LZ_SB(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class NFT_Grain_Employee_TI_LZ_SB : TestEmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               INFT_Grain_Person_TI_LZ_SB, INFT_Grain_Job_TI_LZ_SB
    {
        public NFT_Grain_Employee_TI_LZ_SB(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }
    #endregion // SingleBucket

    public abstract class MultiInterface_TI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_TI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_TI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_PK, FT_Props_Person_TI_LZ_PK,
                                                            IFT_Grain_Job_TI_LZ_PK, FT_Props_Job_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_PK, NFT_Props_Person_TI_LZ_PK,
                                                            INFT_Grain_Job_TI_LZ_PK, NFT_Props_Job_TI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_TI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_SB, FT_Props_Person_TI_LZ_SB,
                                                            IFT_Grain_Job_TI_LZ_SB, FT_Props_Job_TI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_TI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_SB, NFT_Props_Person_TI_LZ_SB,
                                                            INFT_Grain_Job_TI_LZ_SB, NFT_Props_Job_TI_LZ_SB>();
        }

        internal static Func<IndexingTestRunnerBase, int, Task>[] GetAllTestTasks()
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
                (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_PK, FT_Props_Person_TI_LZ_PK,
                                                            IFT_Grain_Job_TI_LZ_PK, FT_Props_Job_TI_LZ_PK>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_PK, NFT_Props_Person_TI_LZ_PK,
                                                            INFT_Grain_Job_TI_LZ_PK, NFT_Props_Job_TI_LZ_PK>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_TI_LZ_SB, FT_Props_Person_TI_LZ_SB,
                                                            IFT_Grain_Job_TI_LZ_SB, FT_Props_Job_TI_LZ_SB>(intAdjust),
                (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_TI_LZ_SB, NFT_Props_Person_TI_LZ_SB,
                                                            INFT_Grain_Job_TI_LZ_SB, NFT_Props_Job_TI_LZ_SB>(intAdjust)
            };
        }
    }
}
