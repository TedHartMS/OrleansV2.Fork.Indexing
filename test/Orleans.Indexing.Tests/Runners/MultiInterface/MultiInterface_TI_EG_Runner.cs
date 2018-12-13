using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Orleans.Indexing.Tests.MultiInterface
{
    // None; Total Index cannot be Eager.

    public abstract class MultiInterface_TI_EG_Runner : IndexingTestRunnerBase
    {
        protected MultiInterface_TI_EG_Runner(BaseIndexingFixture fixture, ITestOutputHelper output)
            : base(fixture, output)
        {
        }

        // None; Total Index cannot be Eager.

        internal static IEnumerable<Func<IndexingTestRunnerBase, int, Task>> GetAllTestTasks(TestIndexPartitionType _)
        {
            return new Func<IndexingTestRunnerBase, int, Task>[]
            {
            };
        }
    }
}
