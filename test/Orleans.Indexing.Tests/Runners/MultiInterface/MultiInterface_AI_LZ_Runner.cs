using Orleans.Providers;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Orleans.Indexing.Facets;
using System.Collections.Generic;

namespace Orleans.Indexing.Tests.MultiInterface
{
    #region PartitionedPerKey

    [Serializable]
    public class FT_Props_Person_AI_LZ_PK : IPersonProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, IFT_Grain_Person_AI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<int, IFT_Grain_Person_AI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class NFT_Props_Person_AI_LZ_PK : IPersonProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, INFT_Grain_Person_AI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<int, INFT_Grain_Person_AI_LZ_PK>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class FT_Props_Job_AI_LZ_PK : IJobProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, IFT_Grain_Job_AI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, IFT_Grain_Job_AI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class NFT_Props_Job_AI_LZ_PK : IJobProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, INFT_Grain_Job_AI_LZ_PK>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(ActiveHashIndexPartitionedPerKey<string, INFT_Grain_Job_AI_LZ_PK>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class FT_Props_Employee_AI_LZ_PK : IEmployeeProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<int, IFT_Grain_Employee_AI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    [Serializable]
    public class NFT_Props_Employee_AI_LZ_PK : IEmployeeProperties
    {
        [Index(typeof(ActiveHashIndexPartitionedPerKey<int, INFT_Grain_Employee_AI_LZ_PK>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface IFT_Grain_Person_AI_LZ_PK : IIndexableGrain<FT_Props_Person_AI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_AI_LZ_PK : IIndexableGrain<NFT_Props_Person_AI_LZ_PK>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_AI_LZ_PK : IIndexableGrain<FT_Props_Job_AI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_AI_LZ_PK : IIndexableGrain<NFT_Props_Job_AI_LZ_PK>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Employee_AI_LZ_PK : IIndexableGrain<FT_Props_Employee_AI_LZ_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_AI_LZ_PK : IIndexableGrain<NFT_Props_Employee_AI_LZ_PK>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class FT_Grain_Employee_AI_LZ_PK : TestEmployeeGrain<EmployeeGrainState, FaultTolerantIndexableGrainStateWrapper<EmployeeGrainState>>,
                                              IFT_Grain_Person_AI_LZ_PK, IFT_Grain_Job_AI_LZ_PK, IFT_Grain_Employee_AI_LZ_PK
    {
        public FT_Grain_Employee_AI_LZ_PK(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class NFT_Grain_Employee_AI_LZ_PK : TestEmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               INFT_Grain_Person_AI_LZ_PK, INFT_Grain_Job_AI_LZ_PK, INFT_Grain_Employee_AI_LZ_PK
    {
        public NFT_Grain_Employee_AI_LZ_PK(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }
    #endregion // PartitionedPerKey

    #region PartitionedPerSilo

    [Serializable]
    public class FT_Props_Person_AI_LZ_PS : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, IFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, IFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class NFT_Props_Person_AI_LZ_PS : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_Person_AI_LZ_PS>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class FT_Props_Job_AI_LZ_PS : IJobProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, IFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, IFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class NFT_Props_Job_AI_LZ_PS : IJobProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexPartitionedPerSilo<string, INFT_Grain_Job_AI_LZ_PS>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class FT_Props_Employee_AI_LZ_PS : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, IFT_Grain_Employee_AI_LZ_PS>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    [Serializable]
    public class NFT_Props_Employee_AI_LZ_PS : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexPartitionedPerSilo<int, INFT_Grain_Employee_AI_LZ_PS>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface IFT_Grain_Person_AI_LZ_PS : IIndexableGrain<FT_Props_Person_AI_LZ_PS>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_AI_LZ_PS : IIndexableGrain<NFT_Props_Person_AI_LZ_PS>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_AI_LZ_PS : IIndexableGrain<FT_Props_Job_AI_LZ_PS>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_AI_LZ_PS : IIndexableGrain<NFT_Props_Job_AI_LZ_PS>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Employee_AI_LZ_PS : IIndexableGrain<FT_Props_Employee_AI_LZ_PS>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_AI_LZ_PS : IIndexableGrain<NFT_Props_Employee_AI_LZ_PS>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class FT_Grain_Employee_AI_LZ_PS : TestEmployeeGrain<EmployeeGrainState, FaultTolerantIndexableGrainStateWrapper<EmployeeGrainState>>,
                                              IFT_Grain_Person_AI_LZ_PS, IFT_Grain_Job_AI_LZ_PS, IFT_Grain_Employee_AI_LZ_PS
    {
        public FT_Grain_Employee_AI_LZ_PS(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class NFT_Grain_Employee_AI_LZ_PS : TestEmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               INFT_Grain_Person_AI_LZ_PS, INFT_Grain_Job_AI_LZ_PS, INFT_Grain_Employee_AI_LZ_PS
    {
        public NFT_Grain_Employee_AI_LZ_PS(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }
    #endregion // PartitionedPerSilo

    #region SingleBucket

    [Serializable]
    public class FT_Props_Person_AI_LZ_SB : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IFT_Grain_Person_AI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<int, IFT_Grain_Person_AI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class NFT_Props_Person_AI_LZ_SB : IPersonProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, INFT_Grain_Person_AI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Name { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<int, INFT_Grain_Person_AI_LZ_SB>), IsEager = false, IsUnique = false, NullValue = "0")]
        public int Age { get; set; }
    }

    [Serializable]
    public class FT_Props_Job_AI_LZ_SB : IJobProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, IFT_Grain_Job_AI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<string, IFT_Grain_Job_AI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class NFT_Props_Job_AI_LZ_SB : IJobProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<string, INFT_Grain_Job_AI_LZ_SB>), IsEager = false, IsUnique = true)]
        public string Title { get; set; }

        [Index(typeof(IActiveHashIndexSingleBucket<string, INFT_Grain_Job_AI_LZ_SB>), IsEager = false, IsUnique = false)]
        public string Department { get; set; }
    }

    [Serializable]
    public class FT_Props_Employee_AI_LZ_SB : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<int, IFT_Grain_Employee_AI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    [Serializable]
    public class NFT_Props_Employee_AI_LZ_SB : IEmployeeProperties
    {
        [Index(typeof(IActiveHashIndexSingleBucket<int, INFT_Grain_Employee_AI_LZ_SB>), IsEager = false, IsUnique = true, NullValue = "-1")]
        public int EmployeeId { get; set; }
    }

    public interface IFT_Grain_Person_AI_LZ_SB : IIndexableGrain<FT_Props_Person_AI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Person_AI_LZ_SB : IIndexableGrain<NFT_Props_Person_AI_LZ_SB>, IPersonGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Job_AI_LZ_SB : IIndexableGrain<FT_Props_Job_AI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Job_AI_LZ_SB : IIndexableGrain<NFT_Props_Job_AI_LZ_SB>, IJobGrain, IGrainWithIntegerKey
    {
    }

    public interface IFT_Grain_Employee_AI_LZ_SB : IIndexableGrain<FT_Props_Employee_AI_LZ_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    public interface INFT_Grain_Employee_AI_LZ_SB : IIndexableGrain<NFT_Props_Employee_AI_LZ_SB>, IEmployeeGrain, IGrainWithIntegerKey
    {
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class FT_Grain_Employee_AI_LZ_SB : TestEmployeeGrain<EmployeeGrainState, FaultTolerantIndexableGrainStateWrapper<EmployeeGrainState>>,
                                              IFT_Grain_Person_AI_LZ_SB, IFT_Grain_Job_AI_LZ_SB, IFT_Grain_Employee_AI_LZ_SB
    {
        public FT_Grain_Employee_AI_LZ_SB(
            [FaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }

    [StorageProvider(ProviderName = IndexingConstants.MEMORY_STORAGE_PROVIDER_NAME)]
    public class NFT_Grain_Employee_AI_LZ_SB : TestEmployeeGrain<EmployeeGrainState, IndexableGrainStateWrapper<EmployeeGrainState>>,
                                               INFT_Grain_Person_AI_LZ_SB, INFT_Grain_Job_AI_LZ_SB, INFT_Grain_Employee_AI_LZ_SB
    {
        public NFT_Grain_Employee_AI_LZ_SB(
            [NonFaultTolerantWorkflowIndexWriter]
            IIndexWriter<EmployeeGrainState> indexWriter)
            : base(indexWriter) { }
    }
    #endregion // SingleBucket

    public abstract class MultiInterface_AI_LZ_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_AI_LZ_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_AI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_AI_LZ_PK, FT_Props_Person_AI_LZ_PK,
                                                            IFT_Grain_Job_AI_LZ_PK, FT_Props_Job_AI_LZ_PK,
                                                            IFT_Grain_Employee_AI_LZ_PK, FT_Props_Employee_AI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_AI_LZ_PK()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_AI_LZ_PK, NFT_Props_Person_AI_LZ_PK,
                                                            INFT_Grain_Job_AI_LZ_PK, NFT_Props_Job_AI_LZ_PK,
                                                            INFT_Grain_Employee_AI_LZ_PK, NFT_Props_Employee_AI_LZ_PK>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_AI_LZ_PS()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_AI_LZ_PS, FT_Props_Person_AI_LZ_PS,
                                                            IFT_Grain_Job_AI_LZ_PS, FT_Props_Job_AI_LZ_PS,
                                                            IFT_Grain_Employee_AI_LZ_PS, FT_Props_Employee_AI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_AI_LZ_PS()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_AI_LZ_PS, NFT_Props_Person_AI_LZ_PS,
                                                            INFT_Grain_Job_AI_LZ_PS, NFT_Props_Job_AI_LZ_PS,
                                                            INFT_Grain_Employee_AI_LZ_PS, NFT_Props_Employee_AI_LZ_PS>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_FT_Grain_Employee_AI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<IFT_Grain_Person_AI_LZ_SB, FT_Props_Person_AI_LZ_SB,
                                                            IFT_Grain_Job_AI_LZ_SB, FT_Props_Job_AI_LZ_SB,
                                                            IFT_Grain_Employee_AI_LZ_SB, FT_Props_Employee_AI_LZ_SB>();
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_NFT_Grain_Employee_AI_LZ_SB()
        {
            await base.TestEmployeeIndexesWithDeactivations<INFT_Grain_Person_AI_LZ_SB, NFT_Props_Person_AI_LZ_SB,
                                                            INFT_Grain_Job_AI_LZ_SB, NFT_Props_Job_AI_LZ_SB,
                                                            INFT_Grain_Employee_AI_LZ_SB, NFT_Props_Employee_AI_LZ_SB>();
        }

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType testIndexTypes)
        {
            if (testIndexTypes.HasFlag(TestIndexPartitionType.PerKeyHash))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            IFT_Grain_Person_AI_LZ_PK, FT_Props_Person_AI_LZ_PK,
                                                            IFT_Grain_Job_AI_LZ_PK, FT_Props_Job_AI_LZ_PK,
                                                            IFT_Grain_Employee_AI_LZ_PK, FT_Props_Employee_AI_LZ_PK>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_AI_LZ_PK, NFT_Props_Person_AI_LZ_PK,
                                                            INFT_Grain_Job_AI_LZ_PK, NFT_Props_Job_AI_LZ_PK,
                                                            INFT_Grain_Employee_AI_LZ_PK, NFT_Props_Employee_AI_LZ_PK>(intAdjust);
            }
            if (testIndexTypes.HasFlag(TestIndexPartitionType.PerSilo))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            IFT_Grain_Person_AI_LZ_PS, FT_Props_Person_AI_LZ_PS,
                                                            IFT_Grain_Job_AI_LZ_PS, FT_Props_Job_AI_LZ_PS,
                                                            IFT_Grain_Employee_AI_LZ_PS, FT_Props_Employee_AI_LZ_PS>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_AI_LZ_PS, NFT_Props_Person_AI_LZ_PS,
                                                            INFT_Grain_Job_AI_LZ_PS, NFT_Props_Job_AI_LZ_PS,
                                                            INFT_Grain_Employee_AI_LZ_PS, NFT_Props_Employee_AI_LZ_PS>(intAdjust);
            }
            if (testIndexTypes.HasFlag(TestIndexPartitionType.SingleBucket))
            {
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            IFT_Grain_Person_AI_LZ_SB, FT_Props_Person_AI_LZ_SB,
                                                            IFT_Grain_Job_AI_LZ_SB, FT_Props_Job_AI_LZ_SB,
                                                            IFT_Grain_Employee_AI_LZ_SB, FT_Props_Employee_AI_LZ_SB>(intAdjust);
                yield return (baseRunner, intAdjust) => baseRunner.TestEmployeeIndexesWithDeactivations<
                                                            INFT_Grain_Person_AI_LZ_SB, NFT_Props_Person_AI_LZ_SB,
                                                            INFT_Grain_Job_AI_LZ_SB, NFT_Props_Job_AI_LZ_SB,
                                                            INFT_Grain_Employee_AI_LZ_SB, NFT_Props_Employee_AI_LZ_SB>(intAdjust);
            }
        }
    }
}
