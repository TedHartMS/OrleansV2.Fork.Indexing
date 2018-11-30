using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public abstract class MultiInterface_All_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_All_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        [Fact, TestCategory("BVT"), TestCategory("Indexing")]
        public async Task Test_MultiInterface_All()
        {
            const int NumRepsPerTest = 3;
            IEnumerable<Task> getTasks(Func<IndexingTestRunnerBase, int, Task>[] getTasksFunc)
            {
                for (int ii = 0; ii < NumRepsPerTest; ++ii)
                {
                    foreach (var task in getTasksFunc.Select(lambda => lambda(this, ii * 1000000)))
                    {
                        yield return task;
                    }
                }
            }

            await Task.WhenAll(getTasks(MultiInterface_AI_EG_Runner.GetAllTestTasks())
                    .Concat(getTasks(MultiInterface_AI_LZ_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiInterface_TI_EG_Runner.GetAllTestTasks()))
                    .Concat(getTasks(MultiInterface_TI_LZ_Runner.GetAllTestTasks())));
        }
    }
}
