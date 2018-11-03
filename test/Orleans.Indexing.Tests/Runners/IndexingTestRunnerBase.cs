using Xunit.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using Orleans.Indexing.Facets;
using Orleans.Indexing.Tests.MultiInterface;
using System.Threading.Tasks;
using System.Linq;
using System;
using Xunit;

namespace Orleans.Indexing.Tests
{
    using ITC = IndexingTestConstants;

    public class IndexingTestRunnerBase
    {
        private BaseIndexingFixture fixture;

        internal readonly ITestOutputHelper Output;
        internal IClusterClient ClusterClient => this.fixture.Client;

        internal IGrainFactory GrainFactory => this.fixture.GrainFactory;

        internal IIndexFactory IndexFactory { get; }

        internal ILoggerFactory LoggerFactory { get; }

        protected TestCluster HostedCluster => this.fixture.HostedCluster;

        protected IndexingTestRunnerBase(BaseIndexingFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.Output = output;
            this.LoggerFactory = this.ClusterClient.ServiceProvider.GetRequiredService<ILoggerFactory>();
            this.IndexFactory = this.ClusterClient.ServiceProvider.GetRequiredService<IIndexFactory>();
        }

        protected TInterface GetGrain<TInterface>(long primaryKey) where TInterface : IGrainWithIntegerKey
            => this.GrainFactory.GetGrain<TInterface>(primaryKey);

        protected TInterface GetGrain<TInterface, TImplClass>(long primaryKey) where TInterface : IGrainWithIntegerKey
            => this.GetGrain<TInterface>(primaryKey, typeof(TImplClass));

        protected TInterface GetGrain<TInterface>(long primaryKey, Type grainImplType) where TInterface : IGrainWithIntegerKey
            => this.GrainFactory.GetGrain<TInterface>(primaryKey, grainImplType.FullName.Replace("+", "."));

        protected IIndexInterface<TKey, TValue> GetIndex<TKey, TValue>(string indexName) where TValue : IIndexableGrain
            => this.IndexFactory.GetIndex<TKey, TValue>(indexName);

        protected async Task<IIndexInterface<TKey, TValue>> GetAndWaitForIndex<TKey, TValue>(string indexName) where TValue : IIndexableGrain
        {
            var locIdx = this.IndexFactory.GetIndex<TKey, TValue>(IndexUtils.PropertyNameToIndexName(indexName));
            while (!await locIdx.IsAvailable()) await Task.Delay(50);
            return locIdx;
        }

        protected async Task<IIndexInterface<TKey, TValue>[]> GetAndWaitForIndexes<TKey, TValue>(params string[] propertyNames) where TValue : IIndexableGrain
        {
            var indexes = propertyNames.Select(name => this.IndexFactory.GetIndex<TKey, TValue>(IndexUtils.PropertyNameToIndexName(name))).ToArray();

            const int MaxRetries = 100;
            int retries = 0;
            foreach (var index in indexes)
            {
                while (!await index.IsAvailable())
                {
                    ++retries;
                    Assert.True(retries < MaxRetries, "Maximum number of GetAndWaitForIndexes retries was exceeded");
                    await Task.Delay(50);
                }
            }
            return indexes;
        }

        internal async Task TestIndexesWithDeactivations<TIGrain, TProperties>(int intAdjust = 0)
            where TIGrain : ITestMultiIndexGrain, IIndexableGrain where TProperties : ITestMultiIndexProperties
        {
            using (var tw = new TestConsoleOutputWriter(this.Output, $"start test: TIGrain = {nameof(TIGrain)}, TProperties = {nameof(TProperties)}"))
            {
                // Use intAdjust to test that different values for the same grain type are handled correctly; see MultiIndex_All.
                var adj1 = intAdjust + 1;
                var adj11 = intAdjust + 11;
                var adj111 = intAdjust + 111;
                var adj1111 = intAdjust + 1111;
                var adj2 = intAdjust + 2;
                var adj3 = intAdjust + 3;
                var adj1000 = intAdjust + 1000;
                var adj2000 = intAdjust + 2000;
                var adj3000 = intAdjust + 3000;
                var adjOne = "one" + intAdjust;
                var adjEleven = "eleven" + intAdjust;
                var adjOneEleven = "oneeleven" + intAdjust;
                var adjElevenEleven = "eleveneleven" + intAdjust;
                var adjTwo = "two" + intAdjust;
                var adjThree = "three" + intAdjust;
                var adj1k = "1k" + intAdjust;
                var adj2k = "2k" + intAdjust;
                var adj3k = "3k" + intAdjust;

                async Task<TIGrain> makeGrain(int uInt, string uString, int nuInt, string nuString)
                {
                    var grain = this.GetGrain<TIGrain>(GrainPkFromUniqueInt(uInt));
                    await grain.SetUniqueInt(uInt);
                    await grain.SetUniqueString(uString);
                    await grain.SetNonUniqueInt(nuInt);
                    await grain.SetNonUniqueString(nuString);
                    return grain;
                }
                var p1 = await makeGrain(adj1, adjOne, adj1000, adj1k);
                var p11 = await makeGrain(adj11, adjEleven, adj1000, adj1k);
                var p111 = await makeGrain(adj111, adjOneEleven, adj1000, adj1k);
                var p1111 = await makeGrain(adj1111, adjElevenEleven, adj1000, adj1k);
                var p2 = await makeGrain(adj2, adjTwo, adj2000, adj2k);
                var p3 = await makeGrain(adj3, adjThree, adj3000, adj3k);

                var intIndexes = await this.GetAndWaitForIndexes<int, TIGrain>(ITC.UniqueIntProperty, ITC.NonUniqueIntProperty);
                var ignoreDeactivate = this.ShouldIgnoreDeactivate(intIndexes[1].GetType());
                var stringIndexes = await this.GetAndWaitForIndexes<string, TIGrain>(ITC.UniqueStringProperty, ITC.NonUniqueStringProperty);

                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjOne));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjEleven));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj2));
                Assert.Equal(1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj3));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjTwo));
                Assert.Equal(1, await this.GetUniqueStringCount<TIGrain, TProperties>(adjThree));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj2000));
                Assert.Equal(1, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj3000));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj2k));
                Assert.Equal(1, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj3k));

                async Task verifyCount(int expected1, int expected11, int expected1000)
                {
                    Assert.Equal(expected1, await this.GetUniqueIntCount<TIGrain, TProperties>(adj1));
                    Assert.Equal(expected11, await this.GetUniqueIntCount<TIGrain, TProperties>(adj11));
                    Assert.Equal(expected1000, await this.GetNonUniqueIntCount<TIGrain, TProperties>(adj1000));
                    Assert.Equal(expected1000, await this.GetNonUniqueStringCount<TIGrain, TProperties>(adj1k));
                }

                Console.WriteLine("*** First Verify ***");
                await verifyCount(1, 1, 4);

                Console.WriteLine("*** First Deactivate ***");
                await p11.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Second Verify ***");
                await verifyCount(1, ignoreDeactivate ? 1 : 0, ignoreDeactivate ? 4 : 3);

                Console.WriteLine("*** Second and Third Deactivate ***");
                await p111.Deactivate();
                await p1111.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Third Verify ***");
                await verifyCount(1, ignoreDeactivate ? 1 : 0, ignoreDeactivate ? 4 : 1);

                Console.WriteLine("*** GetGrain ***");
                p11 = this.GetGrain<TIGrain>(p11.GetPrimaryKeyLong());
                Assert.Equal(adj1000, await p11.GetNonUniqueInt());
                Console.WriteLine("*** Fourth Verify ***");
                await verifyCount(1, 1, ignoreDeactivate ? 4 : 2);
            }
        }

        internal async Task TestEmployeeIndexesWithDeactivations<TIPersonGrain, TPersonProperties, TIJobGrain, TJobProperties>(int intAdjust = 0)
            where TIPersonGrain : IIndexableGrain, IPersonGrain, IGrainWithIntegerKey
            where TPersonProperties: IPersonProperties
            where TIJobGrain : IIndexableGrain, IJobGrain, IGrainWithIntegerKey
            where TJobProperties : IJobProperties
        {
            using (var tw = new TestConsoleOutputWriter(this.Output, $"start test: TIPersonGrain = {nameof(TIPersonGrain)}, TIJobGrain = {nameof(TIJobGrain)}"))
            {
                // Use intAdjust to test that different values for the same grain type are handled correctly; see MultiInterface_All.
                var loc1 = $"location{intAdjust + 1}";
                var loc11 = $"location{intAdjust + 11}";
                var loc111 = $"location{intAdjust + 111}";
                var loc1111 = $"location{intAdjust + 1111}";
                var loc2 = "location2";
                var loc3 = "location3";
                var age1 = intAdjust + 1;
                var age2 = intAdjust + 2;
                var age3 = intAdjust + 3;

                var title1 = $"title{intAdjust + 1}";
                var title11 = $"title{intAdjust + 11}";
                var title111 = $"title{intAdjust + 111}";
                var title1111 = $"title{intAdjust + 1111}";
                var title2 = "title2";
                var title3 = "title3";
                var dept1 = $"department{intAdjust + 1}";
                var dept2 = $"department{intAdjust + 2}";
                var dept3 = $"department{intAdjust + 3}";

                int id = intAdjust * 1000;
                async Task<(TIPersonGrain person, TIJobGrain job)> makeGrain(string location, int age, string title, string dept)
                {
                    var personGrain = this.GetGrain<TIPersonGrain>(GrainPkFromUniqueInt(++id));
                    await personGrain.SetLocation(location);
                    await personGrain.SetAge(age);
                    var jobGrain = personGrain.Cast<TIJobGrain>();
                    await jobGrain.SetTitle(title);
                    await jobGrain.SetDepartment(dept);
                    return (personGrain, jobGrain);
                }
                var p1 = await makeGrain(loc1, age1, title1, dept1);
                var p11 = await makeGrain(loc11, age1, title11, dept1);
                var p111 = await makeGrain(loc111, age1, title111, dept1);
                var p1111 = await makeGrain(loc1111, age1, title1111, dept1);
                var p2 = await makeGrain(loc2, age2, title2, dept2);
                var p3 = await makeGrain(loc3, age3, title3, dept3);

                var personIndexes = await this.GetAndWaitForIndexes<int, TIPersonGrain>(ITC.LocationProperty, ITC.AgeProperty);
                var ignoreDeactivate = this.ShouldIgnoreDeactivate(personIndexes[1].GetType());
                var jobIndexes = await this.GetAndWaitForIndexes<int, TIJobGrain>(ITC.TitleProperty, ITC.DepartmentProperty);
                Assert.Equal(ignoreDeactivate, this.ShouldIgnoreDeactivate(personIndexes[1].GetType()));

                Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc1));
                Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc11));
                Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc2));
                Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc3));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age2));
                Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age3));

                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title1));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title11));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title2));
                Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title3));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept2));
                Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept3));

                async Task verifyCount(int expectedDups)
                {
                    // Verify the duplicated count as well as sanity-checking for some of the non-duplicated ones.
                    Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc1));
                    Assert.Equal(expectedDups == 0 ? 0 : 1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc11));
                    Assert.Equal(1, await this.GetPersonLocationCount<TIPersonGrain, TPersonProperties>(loc2));
                    Assert.Equal(expectedDups, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age1));
                    Assert.Equal(1, await this.GetPersonAgeCount<TIPersonGrain, TPersonProperties>(age2));

                    Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title1));
                    Assert.Equal(1, await this.GetJobTitleCount<TIJobGrain, TJobProperties>(title2));
                    Assert.Equal(expectedDups, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept1));
                    Assert.Equal(1, await this.GetJobDepartmentCount<TIJobGrain, TJobProperties>(dept2));
                }

                Console.WriteLine("*** First Verify ***");
                await verifyCount(4);

                Console.WriteLine("*** First Deactivate ***");
                await p11.person.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Second Verify ***");
                await verifyCount(ignoreDeactivate ? 4 : 3);

                Console.WriteLine("*** Second and Third Deactivate ***");
                await p111.person.Deactivate();
                await p1111.person.Deactivate();
                await Task.Delay(ITC.DelayUntilIndexesAreUpdatedLazily);

                Console.WriteLine("*** Third Verify ***");
                await verifyCount(ignoreDeactivate ? 4 : 1);

                Console.WriteLine("*** GetGrain ***");
                var p11person = this.GetGrain<TIPersonGrain>(p11.person.GetPrimaryKeyLong());
                Assert.Equal(loc11, await p11person.GetLocation());
                var p11job = p11person.Cast<TIJobGrain>();
                Assert.Equal(title11, await p11job.GetTitle());
                Console.WriteLine("*** Fourth Verify ***");
                await verifyCount(ignoreDeactivate ? 4 : 2);
            }
        }

        public static long GrainPkFromUniqueInt(int uInt) => uInt + 4200000000000;

        bool ShouldIgnoreDeactivate(Type nonUniqueIndexType)
            => typeof(ITotalIndex).IsAssignableFrom(nonUniqueIndexType)
                || typeof(IDirectStorageManagedIndex).IsAssignableFrom(nonUniqueIndexType);

        protected Task StartAndWaitForSecondSilo()
        {
            if (this.HostedCluster.SecondarySilos.Count == 0)
            {
                this.HostedCluster.StartAdditionalSilo();
                return this.HostedCluster.WaitForLivenessToStabilizeAsync();
            }
            return Task.CompletedTask;
        }
    }
}
