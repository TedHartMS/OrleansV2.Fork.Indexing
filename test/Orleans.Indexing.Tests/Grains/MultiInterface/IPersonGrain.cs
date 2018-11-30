using System.Threading.Tasks;

namespace Orleans.Indexing.Tests.MultiInterface
{
    public interface IPersonGrain
    {
        Task<string> GetLocation();
        Task SetLocation(string value);

        Task<int> GetAge();
        Task SetAge(int value);

        // For testing
        Task WriteState();
        Task Deactivate();
    }
}
