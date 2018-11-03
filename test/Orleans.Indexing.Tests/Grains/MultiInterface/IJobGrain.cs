using System.Threading.Tasks;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IJobGrain
    {
        Task<string> GetTitle();
        Task SetTitle(string value);

        Task<string> GetDepartment();
        Task SetDepartment(string value);

        Task Deactivate();  // For testing
    }
}
